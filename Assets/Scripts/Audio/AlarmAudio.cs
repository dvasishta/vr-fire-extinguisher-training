using UnityEngine;

// Attach to any GameObject in the scene. Requires AudioSource with alarm WAV, Loop ON.
[RequireComponent(typeof(AudioSource))]
public class AlarmAudio : MonoBehaviour
{
    public float escalationDuration = 120f;

    private AudioSource audioSource;
    private float currentTime;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        InvokeRepeating(nameof(EscalateAlarm), 1f, 1f);
    }

    void EscalateAlarm()
    {
        currentTime += 1f;
        float alpha = Mathf.Clamp01(currentTime / escalationDuration);
        audioSource.pitch = Mathf.Lerp(1.0f, 1.8f, alpha);
        audioSource.volume = Mathf.Lerp(0.4f, 1.0f, alpha);
    }
}
