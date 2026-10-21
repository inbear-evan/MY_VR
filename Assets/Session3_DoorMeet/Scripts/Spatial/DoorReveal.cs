using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Session3DoorMeet
{
    public sealed class DoorReveal : MonoBehaviour
    {
        public DoorSession session;
        public Transform doorRoot;
        public GameObject opaqueDoor, window, demoRoom, missionRoot, pcPartner;
        public EffectMesh roomMesh;
        public OVRPassthroughLayer passthrough;
        public Camera deviceCamera;
        public bool EmergencyView { get; private set; } = true;
        public bool CrossingView { get; private set; }
        public bool IsOpen => session.Connected && session.PartnerPresent.Value && session.DoorUnlocked.Value;
        public bool FullRealView => EmergencyView || CrossingView || !session.Connected;
        bool device;
        void Start() { device=Application.platform==RuntimePlatform.Android; Apply(); }
        public void SetEmergencyView(bool show) { EmergencyView=show; Apply(); }
        void Update()
        {
            if(device)
            {
                // Reveal the whole real room near the swinging door, not just a flat aperture.
                var delta=deviceCamera.transform.position-doorRoot.position;
                float distance=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;
                if(!IsOpen) CrossingView=false;
                else if(distance<1.2f) CrossingView=true;
                else if(distance>1.7f) CrossingView=false;
                if(OVRInput.GetDown(OVRInput.Button.Two)) EmergencyView=!EmergencyView;
            }
            else if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame) EmergencyView=!EmergencyView;
            Apply();
        }
        void Apply()
        {
            bool open=IsOpen;
            opaqueDoor.SetActive(!open && (!device || !FullRealView));
            window.SetActive(device && open && !FullRealView);
            if(pcPartner) pcPartner.SetActive(!device && open);
            if(!device) return;
            if(roomMesh) roomMesh.HideMesh=FullRealView;
            if(demoRoom) demoRoom.SetActive(false);
            if(missionRoot) missionRoot.SetActive(!FullRealView);
            if(deviceCamera)
            {
                deviceCamera.clearFlags=CameraClearFlags.SolidColor;
                deviceCamera.backgroundColor=FullRealView?new Color(0,0,0,0):new Color(.025f,.035f,.06f,1);
            }
        }
    }
}
