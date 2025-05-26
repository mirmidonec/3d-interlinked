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

        if (!hasOpenedDoor && Vector3.Distance(transform.position, doorTarget.position) < interactionDistance)
        {
            hasOpenedDoor = true;
            agent.isStopped = true;

            doorScript.OnInteract();
            StartCoroutine(ContinueAfterDelay(1.0f, chairTarget.position));
        }

        if (hasOpenedDoor && !hasSat && Vector3.Distance(transform.position, chairTarget.position) < interactionDistance)
        {
            hasSat = true;
            agent.isStopped = true;
            HandleSit();
        }
    }

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
        Message = "Talk to Sereja";
        _outline.enabled = true;

    }
     public override void OnLoseFocus()
    {
        _outline.enabled = false;
    }

    public override void OnInteractAlt()
    {
        if (hasSat && device.IsOn)
        {
            player.IsDialogue = true;
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            Dialogue.SetActive(true);
            Message = "";
        }

    }

    public void CloseDialogue()
    {
        player.IsDialogue = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
            Dialogue.SetActive(false);
    }



    private void HandleSit()
    {
        animator.SetBool("isWalking", false);
        animator.SetTrigger("sit");

        transform.position = sitPoint.position;
        transform.rotation = sitPoint.rotation;

        // Add sitting sound
        if (footSource != null && sitClip != null)
        {
            footSource.PlayOneShot(sitClip);
        }

        agent.enabled = false;
    }

    public void TakeDamage()
    {
        if (isDead) return;

        isDead = true;
        animator.SetBool("isWalking", false);
        animator.SetTrigger("die");

        if (agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        Destroy(gameObject, 5f);
    }
}
