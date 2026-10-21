using UnityEngine;
using UnityEngine.Rendering;
namespace Session3DoorMeet
{
    [DefaultExecutionOrder(-500)]
    public sealed class DoorPlatform : MonoBehaviour
    {
        public GameObject questRig, desktopRig;
        public DoorReveal reveal;
        public RenderPipelineAsset scenePipeline;
        RenderPipelineAsset oldPipeline;
        void Awake()
        {
            bool quest=Application.platform==RuntimePlatform.Android;
            oldPipeline=QualitySettings.renderPipeline;
            if(scenePipeline) QualitySettings.renderPipeline=scenePipeline;
            desktopRig.SetActive(!quest);
            questRig.SetActive(quest);
            if(quest)
            {
                var manager=questRig.GetComponent<OVRManager>();
                manager.isInsightPassthroughEnabled=true;
                manager.trackingOriginType=OVRManager.TrackingOrigin.FloorLevel;
                var cam=questRig.GetComponent<OVRCameraRig>().centerEyeAnchor.GetComponent<Camera>();
                cam.clearFlags=CameraClearFlags.SolidColor;
                cam.backgroundColor=new Color(0,0,0,0);
            }
        }
        void OnDestroy() { QualitySettings.renderPipeline=oldPipeline; }
    }
}
