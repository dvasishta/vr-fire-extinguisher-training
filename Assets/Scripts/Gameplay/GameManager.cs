using UnityEngine;
using UnityEngine.SceneManagement;

// Singleton placed in each level scene. Initializes scoring and handles level completion.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Level Config")]
    public int levelIndex = 1;
    public int npcCount = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (ScoringManager.Instance != null)
        {
            ScoringManager.Instance.levelIndex = levelIndex;
            ScoringManager.Instance.InitLevel(npcCount);
        }

        HUDController.Instance?.ShowFuelBar(levelIndex >= 2);
    }

    public void LevelComplete()
    {
        ScoreData data = ScoringManager.Instance?.BuildScoreData() ?? default;
        ScoreScreen.Show(data);
    }

    // Called by ScoreScreen retry button
    public static void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Called by ScoreScreen next-level button
    public static void LoadNextLevel(int currentIndex)
    {
        string next = currentIndex switch
        {
            1 => "Level2",
            2 => "Level3",
            _ => "Tutorial",
        };
        SceneManager.LoadScene(next);
    }

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene("Tutorial");
    }
}
