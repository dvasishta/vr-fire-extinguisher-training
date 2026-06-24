using System.Collections;
using UnityEngine;

// Attach to the extinguisher GameObject alongside OVRGrabbable.
// VRPlayer detects grab via OVRGrabber.grabbedObject and routes trigger to StartSpray/StopSpray.
[RequireComponent(typeof(OVRGrabbable))]
public class FireExtinguisher : MonoBehaviour
{
    [Header("Spray")]
    public float fuelLevel = 1.0f;
    public float drainRate = 0.05f;
    public float sprayRange = 3.0f;
    public Transform nozzleTip;

    [Header("VFX & Audio")]
    public ParticleSystem sprayVFX;
    public AudioSource sprayAudio;

    [HideInInspector] public bool isNearestExtinguisher;

    public bool IsGrabbed { get; private set; }
    public bool IsSpraying { get; private set; }

    private Coroutine sprayCoroutine;
    private OVRGrabbable grabbable;

    void Awake()
    {
        grabbable = GetComponent<OVRGrabbable>();
        gameObject.tag = "Grabbable";
    }

    // Called each frame by VRPlayer when this extinguisher is being held
    public void NotifyGrabbed(bool grabbed)
    {
        if (grabbed == IsGrabbed) return;
        IsGrabbed = grabbed;

        if (!grabbed)
        {
            StopSpray();
        }
        else
        {
            ScoringManager.Instance?.RecordExtinguisherPickup(isNearestExtinguisher);
        }
    }

    public void StartSpray()
    {
        if (fuelLevel <= 0f || !IsGrabbed || IsSpraying) return;
        IsSpraying = true;
        sprayVFX?.Play();
        sprayAudio?.Play();
        sprayCoroutine = StartCoroutine(SprayTick());
    }

    public void StopSpray()
    {
        if (!IsSpraying) return;
        IsSpraying = false;
        sprayVFX?.Stop();
        sprayAudio?.Stop();
        if (sprayCoroutine != null) StopCoroutine(sprayCoroutine);
    }

    IEnumerator SprayTick()
    {
        while (IsSpraying && fuelLevel > 0f)
        {
            fuelLevel = Mathf.Clamp(fuelLevel - drainRate * 0.1f, 0f, 1f);
            HUDController.Instance?.SetFuelLevel(fuelLevel);

            if (nozzleTip != null)
            {
                Ray ray = new Ray(nozzleTip.position, nozzleTip.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, sprayRange))
                {
                    FireActor fire = hit.collider.GetComponentInParent<FireActor>();
                    if (fire != null)
                    {
                        float baseY = fire.transform.position.y;
                        FireHitZone zone;
                        if (hit.point.y < baseY + 0.30f)
                            zone = FireHitZone.Base;
                        else if (hit.point.y > baseY + 0.80f)
                            zone = FireHitZone.Top;
                        else
                            zone = FireHitZone.Middle;

                        fire.ExtinguishHit(zone);
                        ScoringManager.Instance?.RecordSprayHit(zone);
                    }
                }
            }

            if (fuelLevel <= 0f) StopSpray();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
