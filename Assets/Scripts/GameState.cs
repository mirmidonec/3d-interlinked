using System.Collections.Generic;
using UnityEngine;

/// <summary>Локализация одной строкой: Loc.T("Door", "Дверь")</summary>
public static class Loc
{
    public static string T(string en, string ru)
    {
        return PauseMenu.CurrentLanguage == PauseMenu.Language.English ? en : ru;
    }
}

public struct ReportLine
{
    public string Label;
    public int Amount;

    public ReportLine(string label, int amount)
    {
        Label = label;
        Amount = amount;
    }
}

public class DayReport
{
    public bool Correct;
    public bool GameOver;
    public bool RunComplete;
    public bool OutOfMoney;      // причина конца игры: деньги
    public bool OutOfStrikes;    // причина конца игры: 3 ошибки
    public int MoneyBefore;
    public int MoneyAfter;
    public List<ReportLine> Lines = new List<ReportLine>();
}

/// <summary>
/// Всё состояние забега. Статик переживает LoadScene, DontDestroyOnLoad не нужен.
/// ВЕСЬ БАЛАНС ЛЕЖИТ В КОНСТАНТАХ СВЕРХУ — правь числа здесь.
/// </summary>
public static class GameState
{
    // ===== Баланс =====
    public const int TotalDays = 5;
    public const int MaxStrikes = 3;

    public const int StartMoney = 150;
    public const int StartAmmo = 3;
    public const int StartCassettes = 1;
    public const int MaxAmmo = 12;
    public const int MaxCassettes = 5;

    public const int RewardCorrect = 100;        // верный вердикт
    public const int BonusTape = 30;             // верный вердикт + запись голоса проанализирована
    public const int BonusKill = 25;             // репликант устранён
    public const int PenaltyWrong = 40;          // ошибка / нет вердикта
    public const int PenaltyInnocentKilled = 60; // убит человек
    public const int DailyCost = 15;             // расходы отдела каждый день

    public const int AmmoPrice = 10;
    public const int CassettePrice = 35;

    // ===== Состояние забега =====
    public static int Day = 1;
    public static int Money = StartMoney;
    public static int Ammo = StartAmmo;
    public static int Cassettes = StartCassettes;
    public static int Strikes = 0;
    public static int CorrectCount = 0;

    // ===== Флаги текущего дня =====
    public static bool TapeLoaded;
    public static bool TapeAnalyzed;
    public static bool VerdictMade;
    public static bool VerdictSaidReplicant;
    public static bool VerdictCorrect;
    public static bool SuspectKilled;
    public static bool SuspectIsReplicant;   // кто сегодня пришёл (ставит NpcChoiser, читает отчёт)

    private static DayReport _report;

    public static void ResetRun()
    {
        Day = 1;
        Money = StartMoney;
        Ammo = StartAmmo;
        Cassettes = StartCassettes;
        Strikes = 0;
        CorrectCount = 0;
        NpcChoiser.LastIndex = -1;
        BeginDay();
    }

    /// <summary>Вызывается при старте каждого дня (сцена загружена).</summary>
    public static void BeginDay()
    {
        TapeLoaded = false;
        TapeAnalyzed = false;
        VerdictMade = false;
        VerdictSaidReplicant = false;
        VerdictCorrect = false;
        SuspectKilled = false;
        _report = null;
        DBdata.Questions.Clear();   // журнал ответов каждого дня начинается с нуля

        // Страховка от софтлока: репликанта нельзя застрелить без патрона.
        if (Ammo < 1) Ammo = 1;
    }

    public static void NextDay()
    {
        Day++;
    }

    public static void RecordVerdict(bool saidReplicant, bool truthIsReplicant)
    {
        VerdictMade = true;
        VerdictSaidReplicant = saidReplicant;
        VerdictCorrect = saidReplicant == truthIsReplicant;
    }

    // ===== Покупки =====
    public static bool CanBuyAmmo => Money >= AmmoPrice && Ammo < MaxAmmo;
    public static bool CanBuyCassette => Money >= CassettePrice && Cassettes < MaxCassettes;

    public static bool TryBuyAmmo()
    {
        if (!CanBuyAmmo) return false;
        Money -= AmmoPrice;
        Ammo++;
        return true;
    }

    public static bool TryBuyCassette()
    {
        if (!CanBuyCassette) return false;
        Money -= CassettePrice;
        Cassettes++;
        return true;
    }

    public static bool TryLoadTape()
    {
        if (TapeLoaded || Cassettes <= 0) return false;
        Cassettes--;
        TapeLoaded = true;
        return true;
    }

    // ===== Итог дня (считается один раз, потом возвращается из кэша) =====
    public static DayReport SettleDay()
    {
        if (_report != null) return _report;

        var r = new DayReport { MoneyBefore = Money };
        r.Correct = VerdictMade && VerdictCorrect;

        if (r.Correct)
        {
            CorrectCount++;
            r.Lines.Add(new ReportLine(Loc.T("Correct verdict", "Верный вердикт"), RewardCorrect));
            if (TapeAnalyzed)
                r.Lines.Add(new ReportLine(Loc.T("Voice analysis bonus", "Бонус: анализ голоса"), BonusTape));
            if (SuspectKilled)
                r.Lines.Add(new ReportLine(Loc.T("Replicant neutralized", "Репликант устранён"), BonusKill));
        }
        else
        {
            Strikes++;
            r.Lines.Add(new ReportLine(
                VerdictMade ? Loc.T("Wrong verdict", "Ошибочный вердикт")
                            : Loc.T("No verdict", "Вердикт не вынесен"),
                -PenaltyWrong));
            if (SuspectKilled)
                r.Lines.Add(new ReportLine(Loc.T("Innocent killed", "Убит невиновный"), -PenaltyInnocentKilled));
        }

        r.Lines.Add(new ReportLine(Loc.T("Department expenses", "Расходы отдела"), -DailyCost));

        int total = 0;
        foreach (var l in r.Lines) total += l.Amount;

        // В минус уйти нельзя: деньги упираются в ноль.
        Money = Mathf.Max(0, Money + total);
        r.MoneyAfter = Money;

        r.OutOfStrikes = Strikes >= MaxStrikes;
        r.OutOfMoney = Money <= 0;
        r.GameOver = r.OutOfStrikes || r.OutOfMoney;
        r.RunComplete = !r.GameOver && Day >= TotalDays;

        _report = r;
        return r;
    }
}