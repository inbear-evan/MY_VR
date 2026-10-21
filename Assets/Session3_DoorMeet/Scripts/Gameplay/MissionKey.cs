using UnityEngine;
namespace Session3DoorMeet
{
    public sealed class MissionKey : MonoBehaviour
    {
        public int keyId;
        public Vector3 homePosition;
        public Quaternion homeRotation;
        void Awake() { homePosition = transform.position; homeRotation = transform.rotation; }
        public void ResetKey(bool resumeGrab=true)
        {
            var grab=GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if(grab) grab.enabled=false;
            if(grab && grab.isSelected && grab.interactionManager)
                grab.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
            var body = GetComponent<Rigidbody>();
            body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
            body.position=homePosition; body.rotation=homeRotation;
            transform.SetPositionAndRotation(homePosition, homeRotation);
            Physics.SyncTransforms();
            if(grab && resumeGrab) grab.enabled=true;
        }
        public void ResumeGrab() => GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>().enabled=true;
    }
}
