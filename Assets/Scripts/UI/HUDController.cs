using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Attach to the HUD Canvas. Should be set to Screen Space - Camera or World Space for VR.
// Created and shown by VRPlayer on scene load.
public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public Slider fuelBar;
    public GameObject fuelContainer;
    public TextMeshProUGUI hintText;

    private float levelStartTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        levelStartTime = Time.time;
        InvokeRepeating(nameof(UpdateTimer), 1f, 1f);
        if (hintText != null) Invoke(nameof(FadeHint), 5f);
    }

    void UpdateTimer()
    {
        float elapsed = Time.time - levelStartTime;
        int minutes = Mathf.FloorToInt(elapsed / 60f);
        int seconds = Mathf.FloorToInt(elapsed % 60f);
        if (timerText != null)
            timerText.text = $"{minutes}:{seconds:D2}";
    }

    public void SetFuelLevel(float level)
    {
        if (fuelBar != null) fuelBar.value = level;
    }

    public void ShowFuelBar(bool show)
    {
        if (fuelContainer != null) fuelContainer.SetActive(show);
    }

    void FadeHint()
    {
        if (hintText != null)
            hintText.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
