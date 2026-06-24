using System.Collections;
using UnityEngine;

public enum FireHitZone { Base, Top, Middle }

public class FireActor : MonoBehaviour
{
    [Header("Intensity")]
    public float fireIntensity = 0.1f;
    public float maxIntensity = 1.0f;
    public float growthRate = 0.05f;

    [Header("Spread")]
    public float spreadTime = 15.0f;
    public GameObject fireActorPrefab;

    [Header("VFX")]
    public ParticleSystem fireVFX;
    public ParticleSystem smokeVFX;
    public ParticleSystem heatHazeVFX;

    [Header("Colliders")]
    // BaseZone: SphereCollider radius 0.3, local pos Y=0; IsTrigger ON
    // TopZone:  SphereCollider radius 0.3, local pos Y=1.1; IsTrigger ON
    // SpreadDetection: SphereCollider radius 2.0, IsTrigger ON
    public SphereCollider baseZone;
    public SphereCollider topZone;
    public SphereCollider spreadDetection;

    [Header("Audio")]
    public AudioSource fireCrackle;

    public bool IsExtinguished { get; private set; }

    public event System.Action OnFireExtinguished;

    private float spreadTimer;
    private bool canSpread = true;

    void Start()
    {
        InvokeRepeating(nameof(GrowFire), 1f, 1f);
        InvokeRepeating(nameof(CheckSpread), 5f, 5f);
    }

    void GrowFire()
    {
        if (IsExtinguished) return;

        spreadTimer += 1f;
        fireIntensity = Mathf.Clamp(fireIntensity + growthRate, 0f, maxIntensity);
        ApplyVFX();
    }

    void ApplyVFX()
    {
        if (fireVFX != null)
        {
            var emission = fireVFX.emission;
            emission.rateOverTime = fireIntensity * 1000f;
            fireVFX.transform.localScale = Vector3.one * fireIntensity;
        }
        if (fireCrackle != null)
            fireCrackle.volume = 0.3f + fireIntensity * 0.7f;
    }

    void CheckSpread()
    {
        if (!canSpread || IsExtinguished || spreadTimer < spreadTime) return;

        canSpread = false;
        Collider[] hits = Physics.OverlapSphere(transform.position, spreadDetection != null ? spreadDetection.radius : 2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Flammable"))
                Instantiate(fireActorPrefab, hit.transform.position, Quaternion.identity);
        }
    }

    public void ExtinguishHit(FireHitZone zone)
    {
        if (IsExtinguished) return;

        float reduction = (zone == FireHitZone.Base) ? 0.15f : 0.03f;
        fireIntensity = Mathf.Clamp(fireIntensity - reduction, 0f, maxIntensity);

        if (fireIntensity <= 0f)
        {
            IsExtinguished = true;
            fireVFX?.Stop();
            smokeVFX?.Stop();
            heatHazeVFX?.Stop();
            fireCrackle?.Stop();
            OnFireExtinguished?.Invoke();
        }
    }
}
