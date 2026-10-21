using System;
using System.Linq;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Session3DoorMeet
{
    public sealed class RoomAlignment : MonoBehaviour
    {
        public Transform doorRoot, missionRoot;
        public Camera viewCamera;
        public MRUK mruk;
        public EffectMesh roomMesh;
        public DoorReveal reveal;
        public MissionKey[] keys;
        public GameObject demoRoom;
        public DoorSession session;
        public bool Confirmed { get; private set; }
        public string Status { get; private set; } = "PC 모의 공간입니다. 문·책상을 확인하세요.";
        MRUKAnchor[] doors = Array.Empty<MRUKAnchor>();
        int selected;
        public bool DeviceMode => Application.platform == RuntimePlatform.Android;
        public bool HasDoor => !DeviceMode || doors.Length > 0;

        void Start()
        {
            if (DeviceMode)
            {
                Status = "Quest 공간 정보를 불러오는 중입니다.";
                mruk.SceneLoadedEvent.AddListener(Loaded);
                if (mruk.GetCurrentRoom() != null) Loaded();
            }
        }
        void Loaded()
        {
            var room = mruk.GetCurrentRoom();
            if (!room) { Status = "현재 방을 찾지 못했습니다. Quest 공간 설정을 확인하세요."; return; }
            Confirmed = false;
            session.RevokeReady();
            demoRoom.SetActive(false);
            doors = room.Anchors.Where(a => (a.Label & MRUKAnchor.SceneLabels.DOOR_FRAME) != 0 && a.PlaneRect.HasValue).ToArray();
            var table = room.Anchors.FirstOrDefault(a => (a.Label & MRUKAnchor.SceneLabels.TABLE) != 0 && a.VolumeBounds.HasValue);
            if (table)
            {
                var b = table.VolumeBounds.Value;
                float topY = float.MinValue;
                for (int n=0;n<8;n++)
                {
                    var corner = b.center + Vector3.Scale(b.extents, new Vector3((n&1)==0?-1:1,(n&2)==0?-1:1,(n&4)==0?-1:1));
                    topY = Mathf.Max(topY, table.transform.TransformPoint(corner).y);
                }
                var center = table.transform.TransformPoint(b.center);
                missionRoot.position = new Vector3(center.x, topY + .07f, center.z);
                var forward = Vector3.ProjectOnPlane(viewCamera.transform.position-center,Vector3.up).normalized;
                if (forward.sqrMagnitude>.01f) missionRoot.rotation = Quaternion.LookRotation(-forward);
                foreach(var key in keys) { key.homePosition=key.transform.position; key.homeRotation=key.transform.rotation; }
            }
            selected = 0;
            SelectDoor();
            Status = doors.Length == 0 ? "문틀이 없습니다. Quest 공간 설정에서 실제 문을 추가한 뒤 다시 읽으세요." :
                $"문틀 {doors.Length}개 발견. 실제 문과 공개 범위를 확인하세요." + (table ? " 책상 크기도 적용되었습니다." : " 책상 정보가 없어 모의 받침대를 사용합니다.");
            reveal.SetEmergencyView(true);
        }
        public void NextDoor()
        {
            if (doors.Length==0) return;
            Confirmed=false; session.RevokeReady(); selected=(selected+1)%doors.Length; SelectDoor(); reveal.SetEmergencyView(true);
        }
        void SelectDoor()
        {
            if (doors.Length==0) return;
            var anchor=doors[selected]; var rect=anchor.PlaneRect.Value;
            var center=anchor.transform.TransformPoint(new Vector3(rect.center.x,rect.center.y,0));
            var normal=anchor.transform.forward;
            if(Vector3.Dot(normal,viewCamera.transform.position-center)<0) normal=-normal;
            doorRoot.SetPositionAndRotation(center+normal*.04f,Quaternion.LookRotation(normal,Vector3.up));
            // A margin reveals the handle as well as the scanned frame.
            doorRoot.localScale = new Vector3(rect.width+.25f,rect.height+.15f,1);
        }
        public void AdjustDoor(float yaw, float width, float height)
        {
            Confirmed=false;
            session.RevokeReady();
            doorRoot.Rotate(Vector3.up,yaw,Space.World);
            var s=doorRoot.localScale;
            doorRoot.localScale=new Vector3(Mathf.Max(.4f,s.x+width),Mathf.Max(.5f,s.y+height),1);
            reveal.SetEmergencyView(true);
        }
        public void Confirm()
        {
            if (!HasDoor) { Status="실제 문틀을 먼저 등록하세요."; return; }
            Confirmed=true; Status=DeviceMode ? "문·책상 위치 확인 완료. 이제 준비할 수 있습니다." : "PC 모의 정렬 확인 완료. 실제 정렬은 Quest에서 확인합니다.";
            reveal.SetEmergencyView(false);
        }
        public async void Reload()
        {
            if (!DeviceMode) { Confirmed=false; Status="PC는 모의 공간을 사용합니다."; return; }
            Confirmed=false; session.RevokeReady(); reveal.SetEmergencyView(true);
            try { var result=await mruk.LoadSceneFromDevice(); Status="공간 읽기 결과: "+result; }
            catch(Exception e) { Status="공간 읽기 실패: "+e.Message; Debug.LogException(e); }
        }
        void OnDestroy() { if(mruk) mruk.SceneLoadedEvent.RemoveListener(Loaded); }
    }
}
