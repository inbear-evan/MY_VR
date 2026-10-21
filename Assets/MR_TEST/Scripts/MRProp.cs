using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Session2MR
{
    public sealed class MRProp : MonoBehaviour
    {
        public bool correct;
        public Rigidbody body;
        public XRGrabInteractable grab;
        public Vector3 homeLocalPosition;
        public Quaternion homeLocalRotation=Quaternion.identity;
        public Transform homeParent;
        public bool Held => grab && grab.isSelected;
        public void Restore()
        {
            if(Held && grab.interactionManager)grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            if(homeParent)transform.SetParent(homeParent,true);
            var position=homeParent?homeParent.TransformPoint(homeLocalPosition):homeLocalPosition;
            var rotation=homeParent?homeParent.rotation*homeLocalRotation:homeLocalRotation;
            PlaceWorld(position,rotation);
        }
        public void PlaceWorld(Vector3 position,Quaternion rotation)
        {
            bool wasKinematic=body.isKinematic;body.isKinematic=true;
            transform.SetPositionAndRotation(position,rotation);body.position=position;body.rotation=rotation;
            grab.SetTargetPose(new Pose(position,rotation));
            grab.SetTargetLocalScale(transform.localScale);
            body.isKinematic=wasKinematic;
            if(!wasKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
            Physics.SyncTransforms();
        }
    }
}
