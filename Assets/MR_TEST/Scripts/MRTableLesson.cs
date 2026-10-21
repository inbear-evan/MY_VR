using System;
using System.Linq;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.Rendering;

namespace Session2MR
{
    public enum LessonState { Waiting, Ready, Completed }
    [DefaultExecutionOrder(-1000)]
    public sealed class MRTableLesson : MonoBehaviour
    {
        public bool previewOnly;
        public Transform content,platform,goal,doorHinge;
        public GameObject desktopRig,questRig,mockRoom,previewRoom;
        public MRUK mruk;
        public MRProp green,red;
        public Camera pcCamera;
        public Font guiFont;
        public TMP_Text questText;
        public RenderPipelineAsset pipeline;
        public UnityEvent onCompleted=new UnityEvent();
        public LessonState State {get;private set;}=LessonState.Waiting;
        public TablePose Pose {get;private set;}
        public string Status {get;private set;}="공간을 확인한 뒤 단상을 맞추세요.";
        public int CompletionCount {get;private set;}
        public bool DeviceMode => Application.platform==RuntimePlatform.Android && !Application.isEditor;
        public bool Loading {get;private set;}
        public float StableTime {get;private set;}
        RenderPipelineAsset previousPipeline;
        GUIStyle label,button,title;
        float doorAngle;
        bool subscribed;
        void Awake()
        {
            previousPipeline=QualitySettings.renderPipeline;
            if(pipeline)QualitySettings.renderPipeline=pipeline;
            desktopRig.SetActive(!DeviceMode);questRig.SetActive(DeviceMode);
            mockRoom.SetActive(!DeviceMode && !previewOnly);
            if(previewRoom)previewRoom.SetActive(!DeviceMode && previewOnly);
            content.gameObject.SetActive(false);
            if(DeviceMode)
            {
                Status="Quest의 저장된 공간 정보를 읽는 중입니다.";
                mruk.SceneLoadedEvent.AddListener(RoomLoaded);subscribed=true;
                mruk.SceneSettings.DataSource=MRUK.SceneDataSource.Device;
            }
            else Status=previewOnly?"공식 MRUK Office00 방 자산 미리보기 · 실제 공간 인식 아님":"PC 모의 책상 · 실제 Quest 공간 인식과 구별합니다.";
        }
        void Start(){if(DeviceMode && mruk.GetCurrentRoom())RoomLoaded();}
        void RoomLoaded()
        {
            Loading=false;
            if(previewOnly){Status="Quest 공간 정보가 준비되었습니다. 책상·벽 정보를 확인하세요.";return;}
            var room=mruk.GetCurrentRoom();TablePose best=default;float area=0;
            if(room)foreach(var anchor in room.Anchors)
            {
                if(MRTableGeometry.FromAnchor(anchor,out var candidate) && candidate.size.x*candidate.size.y>area)
                {best=candidate;area=candidate.size.x*candidate.size.y;}
            }
            if(area==0){Invalidate("등록된 TABLE 책상이 없습니다. Quest 공간 설정에서 책상을 추가한 뒤 B로 다시 읽으세요.");return;}
            float floorHeight=room && room.FloorAnchor?room.FloorAnchor.transform.position.y:0;
            Fit(best,floorHeight);
        }
        public void FitMock()
        {
            if(DeviceMode)return;
            Fit(new TablePose{center=new Vector3(0,.75f,0),rotation=Quaternion.identity,size=new Vector2(1.6f,.9f),source="PC 모의 책상 · 1.60 × 0.90m"});
        }
        public bool Fit(TablePose pose,float floorHeight=0)
        {
            if(!pose.Valid){Invalidate("책상 크기 정보가 유효하지 않습니다.");return false;}
            Pose=pose;content.SetPositionAndRotation(pose.center,pose.rotation);content.localScale=Vector3.one;
            var forward=Vector3.ProjectOnPlane(pose.rotation*Vector3.forward,Vector3.up).normalized;
            var doorPosition=pose.center+forward*(pose.size.y*.5f+.85f);doorPosition.y=floorHeight;
            doorHinge.parent.SetPositionAndRotation(doorPosition,Quaternion.LookRotation(forward,Vector3.up));
            platform.localScale=new Vector3(pose.size.x,.04f,pose.size.y);
            platform.localPosition=new Vector3(0,.023f,0);
            goal.localPosition=new Vector3(pose.size.x*.27f,.055f,0);
            float goalSize=Mathf.Clamp(Mathf.Min(pose.size.x,pose.size.y)*.45f,.12f,.32f);
            goal.localScale=new Vector3(goalSize,.05f,goalSize);
            green.homeParent=red.homeParent=content;
            green.homeLocalPosition=new Vector3(-pose.size.x*.30f,.17f,-pose.size.y*.20f);
            red.homeLocalPosition=new Vector3(-pose.size.x*.30f,.17f,pose.size.y*.22f);
            green.transform.localScale=red.transform.localScale=Vector3.one*Mathf.Clamp(Mathf.Min(pose.size.x,pose.size.y)*.18f,.07f,.16f);
            content.gameObject.SetActive(true);ResetLesson();
            Status=$"단상 배치 완료 · 가로 {pose.size.x:F2}m / 깊이 {pose.size.y:F2}m. 초록 상자를 흰 받침대에 놓으세요.";
            return true;
        }
        public void Invalidate(string message)
        {
            State=LessonState.Waiting;StableTime=0;CompletionCount=0;doorAngle=0;
            doorHinge.localRotation=Quaternion.identity;content.gameObject.SetActive(false);Status=message;
        }
        public void ResetLesson()
        {
            StableTime=0;CompletionCount=0;doorAngle=0;doorHinge.localRotation=Quaternion.identity;
            State=content.gameObject.activeSelf?LessonState.Ready:LessonState.Waiting;
            green.Restore();red.Restore();Status="초록 상자를 놓고 손을 떼세요. 빨간 상자로는 열리지 않습니다.";
        }
        public bool MeetsGoal(MRProp prop)
        {
            if(State!=LessonState.Ready || !prop || !prop.correct || prop.Held || prop.body.linearVelocity.sqrMagnitude>.0225f)return false;
            var box=prop.GetComponent<BoxCollider>();if(!box)return false;
            Vector3 min=Vector3.one*float.PositiveInfinity,max=Vector3.one*float.NegativeInfinity;
            for(int i=0;i<8;i++)
            {
                var corner=box.center+Vector3.Scale(box.size*.5f,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                var local=content.InverseTransformPoint(prop.transform.TransformPoint(corner));min=Vector3.Min(min,local);max=Vector3.Max(max,local);
            }
            Vector3 target=goal.localPosition;float radius=goal.localScale.x*.5f-.003f;
            float expectedBottom=target.y+.025f;
            float actualBottom=min.y;
            return min.x>target.x-radius && max.x<target.x+radius && min.z>target.z-radius && max.z<target.z+radius &&
                Mathf.Abs(actualBottom-expectedBottom)<.035f;
        }
        public void Evaluate(float dt)
        {
            if(State!=LessonState.Ready)return;
            StableTime=MeetsGoal(green)?StableTime+Mathf.Max(0,dt):0;
            if(StableTime<.5f)return;
            State=LessonState.Completed;CompletionCount++;
            Status="성공! 초록 상자 배치 이벤트로 가상 문이 열립니다.";
            onCompleted.Invoke();
        }
        void FixedUpdate(){Evaluate(Time.fixedDeltaTime);}
        void Update()
        {
            float target=State==LessonState.Completed?-100:0;
            doorAngle=Mathf.MoveTowards(doorAngle,target,Time.deltaTime*100);
            doorHinge.localRotation=Quaternion.Euler(0,doorAngle,0);
            if(DeviceMode)
            {
                if(questText)questText.text="MR · 책상 맞춤과 이벤트\n"+Status+"\n오른손 A: 초기화  /  B: 공간 다시 읽기\n그립: 가상 상자 잡기 → 흰 받침대에서 놓기";
                if(OVRInput.GetDown(OVRInput.Button.One,OVRInput.Controller.RTouch))ResetLesson();
                if(OVRInput.GetDown(OVRInput.Button.Two,OVRInput.Controller.RTouch))ReloadRoom();
            }
        }
        public void PlaceMock(MRProp prop)
        {
            if(DeviceMode || State!=LessonState.Ready)return;
            if(prop.correct)red.Restore();else green.Restore();
            prop.Restore();prop.body.linearVelocity=Vector3.zero;prop.body.angularVelocity=Vector3.zero;
            Vector3 p=goal.localPosition+Vector3.up*(.027f+prop.transform.lossyScale.y*.5f);
            prop.PlaceWorld(content.TransformPoint(p),content.rotation);
            Status=prop.correct?"초록 상자를 올렸습니다. 잠시 기다리면 문이 열립니다.":"빨간 상자는 정답이 아닙니다. 문은 닫힌 상태입니다.";
        }
        public async void ReloadRoom()
        {
            if(!DeviceMode || Loading)return;
            Loading=true;Invalidate("Quest 공간 정보를 다시 읽는 중입니다.");
            try
            {
                var result=await mruk.LoadSceneFromDevice();
                if(this && State==LessonState.Waiting)RoomLoaded();
            }
            catch(Exception e){if(this)Invalidate("공간 읽기 실패: "+e.Message);Debug.LogException(e);}
            finally{if(this)Loading=false;}
        }
        void OnGUI()
        {
            if(DeviceMode)return;
            if(label==null)
            {
                label=new GUIStyle(GUI.skin.label){font=guiFont,fontSize=18,wordWrap=true,richText=false};label.normal.textColor=Color.white;
                title=new GUIStyle(label){fontSize=26};title.normal.textColor=new Color(.3f,.9f,.85f);
                button=new GUIStyle(GUI.skin.button){font=guiFont,fontSize=19,wordWrap=true};
            }
            float scale=Mathf.Min(Screen.width/1600f,Screen.height/1000f);
            GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,Vector3.one*scale);
            GUI.Box(new Rect(15,25,480,925),GUIContent.none);
            GUI.Label(new Rect(35,45,435,70),previewOnly?"MR · 공간 자산 먼저 보기":"MR · 책상 맞춤과 이벤트",title);
            GUI.Label(new Rect(35,120,430,80),"PC 모의 실행 · 실제 카메라/공간 인식 아님",label);
            GUI.Label(new Rect(35,220,430,150),Status,label);
            if(previewOnly)
            {
                GUI.Label(new Rect(35,395,430,260),"공식 MRUK Office00 방 프리팹\n\n책상·벽·바닥은 미리 준비한 모의 자산입니다.\n\n다음 씬: MR_TableEvent\n책상 크기에 맞는 단상과 이벤트를 연습합니다.",label);
                if(!Application.isEditor && GUI.Button(new Rect(35,690,430,60),"책상·이벤트 연습 열기",button))UnityEngine.SceneManagement.SceneManager.LoadScene(0);
            }
            else
            {
                if(GUI.Button(new Rect(35,405,430,60),"1. 모의 책상에 단상 맞추기",button))FitMock();
                GUI.enabled=State==LessonState.Ready;
                if(GUI.Button(new Rect(35,485,430,60),"2. 빨간 상자 올리기 · 오답",button))PlaceMock(red);
                if(GUI.Button(new Rect(35,565,430,60),"3. 초록 상자 올리기 · 정답",button))PlaceMock(green);
                GUI.enabled=true;
                if(GUI.Button(new Rect(35,655,430,60),"처음 상태로",button))ResetLesson();
                GUI.Label(new Rect(35,745,430,155),"책상 위치·크기 → 단상 맞춤\n가상 소품 배치 → 조건 확인 → 문 열기\n\n실물 상자를 자동 인식하거나\n실제 문을 여는 기능은 아닙니다.",label);
                if(!Application.isEditor && GUI.Button(new Rect(35,905,430,35),"공간 예시 먼저 보기",button))UnityEngine.SceneManagement.SceneManager.LoadScene(1);
            }
        }
        void OnDestroy()
        {
            if(subscribed && mruk)mruk.SceneLoadedEvent.RemoveListener(RoomLoaded);
            QualitySettings.renderPipeline=previousPipeline;
        }
    }
}
