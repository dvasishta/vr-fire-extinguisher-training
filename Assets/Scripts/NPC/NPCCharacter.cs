using UnityEngine;
using UnityEngine.AI;

// Requires: NavMeshAgent component, SphereCollider (IsTrigger, radius 2m) named PlayerDetection
[RequireComponent(typeof(NavMeshAgent))]
public class NPCCharacter : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip helpClip;
    public AudioClip thankYouClip;
    public AudioSource audioSource;

    public bool IsRescued { get; private set; }

    private NavMeshAgent agent;
    private Transform followTarget;
    private bool isFollowing;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (isFollowing || IsRescued) return;
        if (!other.CompareTag("VRPlayer")) return;

        isFollowing = true;
        followTarget = other.transform;
        InvokeRepeating(nameof(FollowPlayer), 0f, 0.5f);

        if (audioSource != null && helpClip != null)
            audioSource.PlayOneShot(helpClip);
    }

    void FollowPlayer()
    {
        if (!isFollowing || IsRescued || followTarget == null)
        {
            CancelInvoke(nameof(FollowPlayer));
            return;
        }

        // Check if fire blocks path
        if (Physics.Linecast(transform.position, followTarget.position, out RaycastHit hit))
        {
            if (hit.collider.GetComponentInParent<FireActor>() != null)
            {
                agent.isStopped = true;
                return;
            }
        }

        agent.isStopped = false;
        agent.SetDestination(followTarget.position);
    }

    public void OnRescued()
    {
        if (IsRescued) return;
        IsRescued = true;
        isFollowing = false;
        CancelInvoke(nameof(FollowPlayer));

        agent.isStopped = true;
        ScoringManager.Instance?.RecordNPCRescued();

        if (audioSource != null && thankYouClip != null)
            audioSource.PlayOneShot(thankYouClip);
    }
}
