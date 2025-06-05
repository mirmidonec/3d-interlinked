using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Text;

public class HackerScreen : MonoBehaviour
{
 public TMP_Text hackerText;     // Ссылка на UI элемент
    public float charactersPerSecond = 100f;
    public int maxLines = 100;
        public int maxVisibleLines = 10;

    private List<string> visibleLines = new List<string>();

  private string[] hackerWords = new string[]
  {
    "ACCESS_GRANTED", "TRACE_ROUTE", "127.0.0.1", "FIREWALL_BYPASSED", "PROTOCOL", "ENCRYPTED",
    "AUTH_FAIL", "LOADING", "root@", "SYS_CALL", "PING...", "OVERFLOW", "DECRYPT", "RX_PACKET",
    "MKFS_EXT4", "LOGIN", "PORT_SCAN", "TOKEN", "0xFF", "chmod 777", "passwd", "sudo", "SIGINT",

    "SSH_TUNNEL", "NETSTAT", "STACK_TRACE", "KERNEL_PANIC", "MALLOC", "HEAP_DUMP", "BRUTE_FORCE",
    "NMAP_SCAN", "ARP_SPOOF", "SQL_INJECTION", "XSS_ATTACK", "CSRF_DETECTED", "BASH_HISTORY",
    "wget", "curl", "scp", "vi", "nano", "tcpdump", "strace", "iptables", "systemctl",

    "ICEWEASEL", "TOR_ROUTER", "ZSH_PROMPT", "LOOPBACK", "INIT_D", "DAEMONIZE", "EXPLOIT.KIT",
    "SEGFAULT", "SIGSEGV", "0DAY", "MIRAI", "CVE-2024-XXXX", "METASPLOIT", "RCE_PAYLOAD",
    "BUFFER_OVERFLOW", "DDoS_ACTIVE", "BOTNET_SIGNAL", "REVERSE_SHELL", "CRYPTO_WALLET",
    "KEYLOGGER", "VPN_ACTIVE", "OBFUSCATED_CODE", "BYPASS_UAC", "NO_ROOT", "ECHO_OFF"
  };

    private StringBuilder textBuffer = new StringBuilder();
    private float timer = 0f;

     void Update()
    {
        timer += Time.deltaTime;
        int charsToAdd = Mathf.FloorToInt(timer * charactersPerSecond);
        timer -= charsToAdd / charactersPerSecond;

        for (int i = 0; i < charsToAdd; i++)
        {
            string line = GenerateLine();
            visibleLines.Add(line);

            // Удалить лишние строки
            if (visibleLines.Count > maxVisibleLines)
                visibleLines.RemoveAt(0);
        }

        // Обновить текст
        hackerText.text = string.Join("\n", visibleLines);
    }

    string GenerateLine()
    {
        int choice = Random.Range(0, 4);
        switch (choice)
        {
            case 0:
                return hackerWords[Random.Range(0, hackerWords.Length)];

            case 1:
                int len = Random.Range(20, 40);
                string s = "";
                for (int i = 0; i < len; i++)
                {
                    float r = Random.value;
                    char c = r < 0.4f ? (char)Random.Range(48, 58) : // цифры
                             r < 0.7f ? (char)Random.Range(65, 91) : // заглавные
                                        (char)Random.Range(33, 48);  // спецсимволы
                    s += c;
                }
                return s;

            case 2:
                string hex = "0x";
                for (int i = 0; i < Random.Range(6, 10); i++)
                    hex += Random.Range(0, 16).ToString("X");
                return hex;

            case 3:
                return Random.value > 0.5f
                    ? $"{Random.Range(0, 256)}.{Random.Range(0, 256)}.{Random.Range(0, 256)}.{Random.Range(0, 256)}"
                    : System.DateTime.Now.AddSeconds(-Random.Range(0, 100000))
                        .ToString("yyyy-MM-dd HH:mm:ss");

            default:
                return "???";
        }
    }
}

