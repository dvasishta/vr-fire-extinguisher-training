using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

// Attach to the Canvas root of the tutorial panel.
// Assign all UI fields in the Inspector.
public class TutorialPanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public RawImage slideImage;
    public Button nextButton;
    public Button startButton;
    public Image[] dotImages;

    [Header("Slides")]
    public List<TutorialSlide> slides = new();

    private int slideIndex;

    void Start()
    {
        if (slides.Count == 0) PopulateDefaultSlides();
        nextButton.onClick.AddListener(OnNext);
        startButton.onClick.AddListener(OnStart);
        UpdateDisplay();
    }

    void PopulateDefaultSlides()
    {
        slides.Add(new TutorialSlide
        {
            title = "Welcome to Fire Safety VR",
            body = "In this training you will learn how to respond to a fire emergency. Follow all instructions carefully.",
        });
        slides.Add(new TutorialSlide
        {
            title = "RACE — The 4 Steps",
            body = "R — Rescue anyone in danger\nA — Alert others and pull the alarm\nC — Contain by closing doors\nE — Extinguish only if safe",
        });
        slides.Add(new TutorialSlide
        {
            title = "PASS — How to Use an Extinguisher",
            body = "P — Pull the pin\nA — Aim at the BASE of the fire\nS — Squeeze the handle\nS — Sweep side to side",
        });
        slides.Add(new TutorialSlide
        {
            title = "Always Aim at the BASE",
            body = "The most common mistake is aiming at the flames. Always aim the nozzle at the BASE of the fire — this cuts off the fuel source and extinguishes it 5x faster.",
        });
        slides.Add(new TutorialSlide
        {
            title = "Emergency Exits",
            body = "Always locate the emergency exit when you enter a room. Look for green EXIT signs. Do not use elevators. Never re-enter a burning building.",
        });
        slides.Add(new TutorialSlide
        {
            title = "You're Ready!",
            body = "Let's practice what you've learned in 3 real scenarios.",
        });
    }

    void OnNext()
    {
        slideIndex = Mathf.Min(slideIndex + 1, slides.Count - 1);
        UpdateDisplay();
    }

    void OnStart()
    {
        SceneManager.LoadScene("Level1");
    }

    void UpdateDisplay()
    {
        var slide = slides[slideIndex];
        titleText.text = slide.title;
        bodyText.text = slide.body;
        if (slideImage != null && slide.image != null)
            slideImage.texture = slide.image;

        for (int i = 0; i < dotImages.Length; i++)
            dotImages[i].color = new Color(1, 1, 1, i <= slideIndex ? 1f : 0.3f);

        bool isLast = slideIndex >= slides.Count - 1;
        nextButton.gameObject.SetActive(!isLast);
        startButton.gameObject.SetActive(isLast);
    }
}

[System.Serializable]
public class TutorialSlide
{
    public string title;
    [TextArea(3, 6)] public string body;
    public Texture2D image;
}
