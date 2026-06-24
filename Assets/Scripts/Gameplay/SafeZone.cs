using UnityEngine;

// Add BoxCollider (IsTrigger ON) to cover the exit doorway.
// Set bIsCorrectExit=true only on the real emergency exit.
public class SafeZone : MonoBehaviour
{
    public bool bIsCorrectExit;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("VRPlayer"))
        {
            ScoringManager.Instance?.RecordExitUsed(bIsCorrectExit);
            if (bIsCorrectExit)
                GameManager.Instance?.LevelComplete();
        }
        else
        {
            NPCCharacter npc = other.GetComponent<NPCCharacter>();
            if (npc != null) npc.OnRescued();
        }
    }
}
