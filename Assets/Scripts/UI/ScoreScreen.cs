using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to the Score Screen Canvas (hidden by default, shown by GameManager.LevelComplete).
public class ScoreScreen : MonoBehaviour
{
    public static ScoreScreen Instance { get; private set; }

    [Header("Header")]
    public TextMeshProUGUI levelCompleteText;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI feedbackText;

    [Header("Score Rows")]
    public TextMeshProUGUI timeRow;
    public TextMeshProUGUI extinguisherRow;
    public TextMeshProUGUI techniqueRow;
    public TextMeshProUGUI exitRow;
    public TextMeshProUGUI npcRow;

    [Header("Row Containers (hide for levels that don't use them)")]
    public GameObject extinguisherContainer;
    public GameObject techniqueContainer;
    public GameObject npcContainer;

    [Header("Buttons")]
    public Button retryButton;
    public Button nextLevelButton;
    public Button mainMenuButton;

    private int cachedLevelIndex;

    void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    public static void Show(ScoreData data)
    {
        if (Instance == null) return;
        Instance.gameObject.SetActive(true);
        Instance.Populate(data);
    }

    void Populate(ScoreData d)
    {
        cachedLevelIndex = d.levelIndex;

        levelCompleteText.text = $"Level {d.levelIndex} Complete!";

        timeRow.text = $"Time: {d.timeTaken:F0}s  —  {d.timePoints} pts";
        exitRow.text = $"Exit: {(d.exitCorrect ? "Correct" : "Wrong")}  —  {d.exitPoints} pts";

        bool showExt = d.levelIndex >= 2;
        extinguisherContainer?.SetActive(showExt);
        techniqueContainer?.SetActive(showExt);
        if (showExt)
        {
            extinguisherRow.text = $"Extinguisher: {(d.usedNearest ? "Nearest" : "Not nearest")}  —  {(d.usedNearest ? 10 : 0)} pts";
            techniqueRow.text = $"Technique: {d.techniquePercent:F0}% base hits  —  {d.techniquePoints} pts";
        }

        bool showNPC = d.levelIndex == 3;
        npcContainer?.SetActive(showNPC);
        if (showNPC)
            npcRow.text = $"NPCs Rescued: {d.npcsRescued}/{d.npcsTotal}  —  {d.npcPoints} pts";

        totalScoreText.text = d.grandTotal.ToString();
        totalScoreText.color = d.grandTotal >= 80 ? new Color(1f, 0.647f, 0f) :
                               d.grandTotal >= 50 ? new Color(0.753f, 0.753f, 0.753f) :
                                                    new Color(0.804f, 0.498f, 0.196f);

        feedbackText.text = d.grandTotal >= 80
            ? "Excellent! You handled this emergency perfectly."
            : d.grandTotal >= 50
            ? "Good job! Practice aiming at the base of the fire."
            : "Keep practicing. Remember: aim at the BASE, pick the NEAREST extinguisher.";

        retryButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(GameManager.ReloadScene);

        nextLevelButton.onClick.RemoveAllListeners();
        nextLevelButton.onClick.AddListener(() => GameManager.LoadNextLevel(d.levelIndex));
        nextLevelButton.gameObject.SetActive(d.levelIndex < 3);

        mainMenuButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.AddListener(GameManager.LoadMainMenu);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
