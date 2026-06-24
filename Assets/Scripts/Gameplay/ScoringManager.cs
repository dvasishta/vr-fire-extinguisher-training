using UnityEngine;

// Singleton. Persists across scenes via DontDestroyOnLoad.
public class ScoringManager : MonoBehaviour
{
    public static ScoringManager Instance { get; private set; }

    [HideInInspector] public int levelIndex = 1;

    private float levelStartTime;
    private int totalSprayHits;
    private int baseSprayHits;
    private bool pickedUpNearestExtinguisher;
    private bool correctExitUsed;
    private int npcsRescued;
    private int npcsTotal;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InitLevel(int npcCount)
    {
        levelStartTime = Time.time;
        npcsTotal = npcCount;
        totalSprayHits = 0;
        baseSprayHits = 0;
        pickedUpNearestExtinguisher = false;
        correctExitUsed = false;
        npcsRescued = 0;
    }

    public void RecordExtinguisherPickup(bool wasNearest)
    {
        pickedUpNearestExtinguisher = wasNearest;
    }

    public void RecordSprayHit(FireHitZone zone)
    {
        totalSprayHits++;
        if (zone == FireHitZone.Base) baseSprayHits++;
    }

    public void RecordNPCRescued()
    {
        npcsRescued++;
    }

    public void RecordExitUsed(bool correct)
    {
        correctExitUsed = correct;
    }

    public int CalculateFinalScore()
    {
        float timeTaken = Time.time - levelStartTime;
        float timeScore = Mathf.Clamp((120f - timeTaken) / 120f * 40f, 0f, 40f);

        int safeHits = Mathf.Max(totalSprayHits, 1);
        float baseRatio = (float)baseSprayHits / safeHits;
        float techScore = baseRatio * 30f;

        float exitScore = correctExitUsed ? 20f : 0f;
        float extScore = pickedUpNearestExtinguisher ? 10f : 0f;

        int safeNPCs = Mathf.Max(npcsTotal, 1);
        float npcScore = (float)npcsRescued / safeNPCs * 20f;

        float total = levelIndex switch
        {
            1 => timeScore + exitScore,
            2 => timeScore + techScore + exitScore + extScore,
            _ => timeScore + techScore + npcScore + extScore,
        };

        return Mathf.FloorToInt(total);
    }

    // Called by SafeZone / GameManager after level ends
    public ScoreData BuildScoreData()
    {
        float timeTaken = Time.time - levelStartTime;
        int safeHits = Mathf.Max(totalSprayHits, 1);
        float baseRatio = (float)baseSprayHits / safeHits;

        return new ScoreData
        {
            levelIndex = levelIndex,
            timeTaken = timeTaken,
            timePoints = Mathf.FloorToInt(Mathf.Clamp((120f - timeTaken) / 120f * 40f, 0f, 40f)),
            usedNearest = pickedUpNearestExtinguisher,
            techniquePercent = baseRatio * 100f,
            techniquePoints = Mathf.FloorToInt(baseRatio * 30f),
            exitCorrect = correctExitUsed,
            exitPoints = correctExitUsed ? 20 : 0,
            npcsRescued = npcsRescued,
            npcsTotal = npcsTotal,
            npcPoints = Mathf.FloorToInt((float)npcsRescued / Mathf.Max(npcsTotal, 1) * 20f),
            grandTotal = CalculateFinalScore(),
        };
    }
}

[System.Serializable]
public struct ScoreData
{
    public int levelIndex;
    public float timeTaken;
    public int timePoints;
    public bool usedNearest;
    public float techniquePercent;
    public int techniquePoints;
    public bool exitCorrect;
    public int exitPoints;
    public int npcsRescued;
    public int npcsTotal;
    public int npcPoints;
    public int grandTotal;
}
