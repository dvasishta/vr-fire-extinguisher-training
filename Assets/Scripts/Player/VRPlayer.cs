using UnityEngine;

// Attach to the OVRCameraRig root (or its child that represents the player body).
// Assign rightGrabber and leftGrabber to the OVRGrabber components on each hand.
// This script routes trigger input to the currently held FireExtinguisher.
public class VRPlayer : MonoBehaviour
{
    public OVRGrabber rightGrabber;
    public OVRGrabber leftGrabber;

    private FireExtinguisher heldExtinguisher;

    void Awake()
    {
        gameObject.tag = "VRPlayer";
    }

    void Update()
    {
        TrackGrab(rightGrabber, OVRInput.Axis1D.SecondaryIndexTrigger);
        TrackGrab(leftGrabber, OVRInput.Axis1D.PrimaryIndexTrigger);
    }

    void TrackGrab(OVRGrabber grabber, OVRInput.Axis1D triggerAxis)
    {
        if (grabber == null) return;

        OVRGrabbable grabbed = grabber.grabbedObject;
        FireExtinguisher ext = grabbed != null ? grabbed.GetComponent<FireExtinguisher>() : null;

        if (ext != heldExtinguisher)
        {
            heldExtinguisher?.NotifyGrabbed(false);
            heldExtinguisher = ext;
            heldExtinguisher?.NotifyGrabbed(true);
        }

        if (heldExtinguisher != null)
        {
            float trigger = OVRInput.Get(triggerAxis);
            if (trigger > 0.1f)
                heldExtinguisher.StartSpray();
            else if (trigger < 0.05f)
                heldExtinguisher.StopSpray();
        }
    }
}
