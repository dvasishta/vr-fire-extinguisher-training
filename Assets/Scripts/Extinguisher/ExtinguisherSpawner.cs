using UnityEngine;

// Place one of these in Level2 and Level3.
// At start it finds all FireActors and FireExtinguishers and marks the nearest extinguisher.
public class ExtinguisherSpawner : MonoBehaviour
{
    void Start()
    {
        // Short delay so all actors have initialized
        Invoke(nameof(FindNearest), 0.5f);
    }

    void FindNearest()
    {
        FireActor[] fires = FindObjectsOfType<FireActor>();
        FireExtinguisher[] extinguishers = FindObjectsOfType<FireExtinguisher>();

        if (fires.Length == 0 || extinguishers.Length == 0) return;

        Vector3 firePos = fires[0].transform.position;
        FireExtinguisher nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var ext in extinguishers)
        {
            float dist = Vector3.Distance(ext.transform.position, firePos);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = ext;
            }
        }

        if (nearest != null)
            nearest.isNearestExtinguisher = true;
    }
}
