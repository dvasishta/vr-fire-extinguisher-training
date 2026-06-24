// Stub implementations of Meta OVR classes for editor testing without Meta XR SDK.
// In editor: left-click near an extinguisher to grab it, hold Space to spray.
// Replace this file with the real Meta XR SDK when deploying to Quest.
using UnityEngine;

public class OVRGrabbable : MonoBehaviour { }

public class OVRGrabber : MonoBehaviour
{
    public OVRGrabbable grabbedObject { get; private set; }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            if (grabbedObject == null)
            {
                Collider[] cols = Physics.OverlapSphere(transform.position, 1.5f);
                foreach (var col in cols)
                {
                    var g = col.GetComponent<OVRGrabbable>();
                    if (g != null) { grabbedObject = g; break; }
                }
            }
        }
        else
        {
            grabbedObject = null;
        }
    }
}

public static class OVRInput
{
    public enum Axis1D { PrimaryIndexTrigger, SecondaryIndexTrigger }

    public static float Get(Axis1D axis)
    {
        return Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }
}
