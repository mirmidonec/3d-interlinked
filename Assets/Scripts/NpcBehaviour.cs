using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NpcBehaviour : Interactable
{
    public Transform doorTarget;
    public Transform chairTarget;
    public Transform sitPoint;

    private NavMeshAgent agent;
    private Animator animator;

    private bool hasOpenedDoor = false;
    public bool hasSat = false;
    public bool isDead = false;

    [SerializeField] private float interactionDistance;
    [SerializeField] private Door doorScript;

    [Header("Audio Settings")]
    public AudioSource footSource;
    public AudioClip[] walkClips;
    public AudioClip sitClip;
    private float footstepTimer = 0f;
    private float baseStepSpeed = 0.5f;

    public Device device;

    public PlayerController player;

    public GameObject Dialogue;

    private BoxCollider _box;
    public string Condition;
    public bool IsReplicant;

    protected override void Awake()
    {
        base.Awake();
        _box = GetComponent<BoxCollider>();
    }


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (!isDead)
        {
            agent.SetDestination(doorTarget.position);
        }
    }

    void Update()
    {
        if (isDead) return;

        animator.SetBool("isWalking", agent.velocity.magnitude > 0.1f);
        HandleFootsteps();
        if (isGoingBack)
        {
            return;
        }
        if (!hasOpenedDoor && Vector3.Distance(transform.position, doorTarget.position) < interactionDistance)
        {
            hasOpenedDoor = true;
            agent.isStopped = true;
            if (!doorScript.IsOpen)
            {
                doorScript.OnInteract();
            }
            StartCoroutine(ContinueAfterDelay(1.0f, chairTarget.position));
        }

        if (hasOpenedDoor && !hasSat && Vector3.Distance(transform.position, chairTarget.position) < interactionDistance)
        {
            hasSat = true;
            agent.isStopped = true;
            HandleSit();
        }
    }
    public Ending end;

    private void HandleFootsteps()
    {
        if (agent.velocity.magnitude < 0.1f || isDead) return;

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            if (footSource != null && walkClips != null && walkClips.Length > 0)
            {
                footSource.pitch = Random.Range(0.8f, 1.5f);
                footSource.PlayOneShot(walkClips[Random.Range(0, walkClips.Length)]);
                footstepTimer = baseStepSpeed;
            }
        }
    }

    private IEnumerator ContinueAfterDelay(float delay, Vector3 nextTarget)
    {
        yield return new WaitForSeconds(delay);
        if (!isDead)
        {
            agent.isStopped = false;
            agent.SetDestination(nextTarget);
        }
    }

    public override void OnFocus()
    {
        Message = PauseMenu.CurrentLanguage == PauseMenu.Language.English
      ? "Talk to suspect"
      : "Поговорить с подозреваемым";
        _outline.enabled = true;

    }
    public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }

    public DialogueSystem dialogue;
    public override void OnInteractAlt()
    {
        if (hasSat && device.IsOn && player.isSitting && !dialogue.IsDone)
        {
            player.IsDialogue = true;
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            Dialogue.SetActive(true);
            Message = "";
        }
        else if (dialogue.IsDone && !DialogueSystem.instance.iScHOICErEPLICANT)
        {
            Message = PauseMenu.CurrentLanguage == PauseMenu.Language.English
                ? "You can go home now"
                : "Можешь идти домой";

            QuestManager.Instance.CompleteStep(0);
            StandUpAndLeave();
        } else if (dialogue.IsDone && DialogueSystem.instance.iScHOICErEPLICANT)
        {
             Message = PauseMenu.CurrentLanguage == PauseMenu.Language.English
                ? "I need to kill him before he realizes."
                : "Можешь идти домой";
        }
        else
        {
            Message = PauseMenu.CurrentLanguage == PauseMenu.Language.English
                ? "Can't do this now"
                : "Сейчас это сделать нельзя";
        }

    }

    public void CloseDialogue()
    {
        player.IsDialogue = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Dialogue.SetActive(false);
    }
    public void StandUpAndLeave()
    {
        if (isDead) return;

        StopAllCoroutines();

        if (!agent.enabled)
            agent.enabled = true;

        hasSat = false;
        hasOpenedDoor = false;
        StartCoroutine(StandUpAndGoBackRoutine());
    }

    public Transform startPos;
    public bool isGoingBack = false;

    private IEnumerator StandUpAndGoBackRoutine()
    {
        isGoingBack = true;
        animator.SetBool("isWalking", true);
        transform.position = sitPoint.position + sitPoint.forward * 0.5f;

        agent.isStopped = false;
        agent.SetDestination(doorTarget.position);

        while (Vector3.Distance(transform.position, doorTarget.position) > interactionDistance)
        {
            animator.SetBool("isWalking", agent.velocity.magnitude > 0.1f);
            yield return null;
        }

        if (!doorScript.IsOpen)
        {
            doorScript.OnInteract();
            yield return new WaitForSeconds(1f);
        }

        agent.SetDestination(startPos.position);
        yield return new WaitForSeconds(0.5f);
        doorScript.OnInteract();
        while (Vector3.Distance(transform.position, startPos.position) > 0.1f)
        {
            animator.SetBool("isWalking", agent.velocity.magnitude > 0.1f);
            yield return null;
        }

       animator.SetBool("isWalking", false);
if (DayUI.Instance != null) DayUI.Instance.FinishDay(0.3f);
else CameraEffectManager.Instance.StartEnd();
Destroy(gameObject);
    }

public AudioSource source2;
public AudioClip chase;
public AudioClip final;
public void StandUpAndChasePlayer()
{
    if (isDead) return;

    StopAllCoroutines();

    if (!agent.enabled)
        agent.enabled = true;

    isGoingBack = false;
    hasSat = false;
    hasOpenedDoor = true;

    transform.position = sitPoint.position + sitPoint.forward * 0.5f;
    transform.rotation = sitPoint.rotation;

    agent.isStopped = false;
    agent.SetDestination(player.transform.position);
    source2.clip = chase;
    source2.Play();

    StartCoroutine(ChasePlayerRoutine());
}

[SerializeField] private float catchDistance = 1.5f;

private IEnumerator ChasePlayerRoutine()
{
    while (!isDead)
    {
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.SetDestination(player.transform.position);

            animator.SetBool("isWalking", agent.velocity.magnitude > 0.1f);

            if (Vector3.Distance(transform.position, player.transform.position) <= catchDistance)
            {
                agent.isStopped = true;
                animator.SetBool("isWalking", false);

                OnPlayerCaught(); // Твой метод при поимке

                yield break;
            }
        }

        yield return new WaitForSeconds(0.2f);
    }
}

private void OnPlayerCaught()
{
    source2.Stop();
    source2.PlayOneShot(final);
    end.StartEnd();
}

    private void HandleSit()
    {
        animator.SetBool("isWalking", false);
        animator.SetTrigger("sit");

        transform.position = sitPoint.position;
        transform.rotation = sitPoint.rotation;

        _box.size = new Vector3(2f, 7.3f, 2f);
        _box.center = new Vector3(-0.1f, 3f, 0.02f);


        if (footSource != null && sitClip != null)
        {
            footSource.PlayOneShot(sitClip);
        }

        agent.enabled = false;

        QuestManager.Instance.CompleteStep(0);
        PlayerEntity.Instance.InteractDistance = 4.5f;
    }
    public AudioClip dying;
    public void TakeDamage()
    {
        if (isDead) return;

        isDead = true;
        GameState.SuspectKilled = true;
        animator.SetBool("isWalking", false);
        animator.SetTrigger("die");
        footSource.PlayOneShot(dying);
        QuestManager.Instance.CompleteStep(1);

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        if (DayUI.Instance != null) DayUI.Instance.FinishDay(1.6f);
else CameraEffectManager.Instance.StartEnd();
        Destroy(gameObject, 5f);
    }
}
