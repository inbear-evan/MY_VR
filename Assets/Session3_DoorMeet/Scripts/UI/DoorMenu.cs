using System.Linq;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Session3DoorMeet
{
    public sealed class DoorMenu : MonoBehaviour
    {
        public DoorSession session;
        public RoomAlignment alignment;
        public DoorReveal reveal;
        public KeySocketGoal goal;
        public MissionKey[] keys;
        public TMP_FontAsset font;
        public Camera questCamera;
        RectTransform panel, body;
        TextMeshProUGUI status, addressLabel, codeLabel, clue, missionState, spaceState;
        string address="192.168.0.10", code="";
        int page;
        float y;
        bool device;
        int lastRound=-1;
        int selectedKey=1;
        bool lastCrossing;
        public string LocalIP { get; private set; }="127.0.0.1";

        void Start()
        {
            device=Application.platform==RuntimePlatform.Android;
            try
            {
                LocalIP=string.Join(" / ", Dns.GetHostEntry(Dns.GetHostName()).AddressList
                    .Where(a=>a.AddressFamily==AddressFamily.InterNetwork && !IPAddress.IsLoopback(a)).Select(a=>a.ToString()));
            }
            catch { LocalIP="주소를 확인하세요"; }
            var canvasObject=new GameObject("DoorMeet Menu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();
            if(device)
            {
                canvas.renderMode=RenderMode.WorldSpace; canvas.worldCamera=questCamera;
                canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                var rect=canvasObject.GetComponent<RectTransform>(); rect.sizeDelta=new Vector2(620,900);
                canvasObject.transform.SetParent(questCamera.transform,false);
                canvasObject.transform.localPosition=new Vector3(0,-.05f,1.3f);
                canvasObject.transform.localScale=Vector3.one*.00125f;
            }
            else
            {
                canvas.renderMode=RenderMode.ScreenSpaceOverlay;
                canvasObject.AddComponent<GraphicRaycaster>();
                var scaler=canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1600,1000); scaler.matchWidthOrHeight=1;
            }
            panel=new GameObject("Panel",typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();
            panel.SetParent(canvasObject.transform,false);
            panel.sizeDelta=new Vector2(620,900);
            if(!device) { panel.anchorMin=panel.anchorMax=new Vector2(0,.5f); panel.pivot=new Vector2(0,.5f); panel.anchoredPosition=new Vector2(20,0); }
            panel.GetComponent<Image>().color=new Color(.025f,.045f,.075f,.97f);
            body=panel; y=18;
            Label("DOOR MEET · 3회차",30,40,new Color(.3f,.85f,.85f));
            status=Label("",19,76,Color.white);
            Row(new[]{"① 연결","② 미션","③ 공간 맞춤"},new UnityAction[]{()=>Show(0),()=>Show(1),()=>Show(2)},42);
            body=new GameObject("Page",typeof(RectTransform)).GetComponent<RectTransform>(); body.SetParent(panel,false);
            body.anchorMin=body.anchorMax=new Vector2(.5f,1); body.pivot=new Vector2(.5f,1);
            body.anchoredPosition=new Vector2(0,-190); body.sizeDelta=new Vector2(600,615);
            Show(0);
            var saved=body; body=panel; y=826;
            Row(new[]{"현실 전체 보기 / 돌아가기",device?"메뉴 숨기기 · 왼쪽 X로 복귀":"연결 종료"},new UnityAction[]{()=>reveal.SetEmergencyView(!reveal.EmergencyView),()=>{if(device)panel.gameObject.SetActive(false);else session.Leave();}},48);
            body=saved;
            session.Changed+=Refresh;
        }
        public void OpenPage(int index) { if(index>=0&&index<=2) Show(index); }
        void Show(int index)
        {
            page=index;
            foreach(Transform child in body) Destroy(child.gameObject);
            y=0; addressLabel=codeLabel=clue=missionState=spaceState=null;
            if(index==0)
            {
                Label("같은 Wi-Fi에 Quest 두 대를 연결하세요.\nA는 방을 만들고, B는 A의 주소로 참가합니다.",22,62,Color.white);
                Label("이 기기의 주소: "+LocalIP+"\nUDP 포트: "+DoorSession.Port,20,55,new Color(.6f,.8f,1));
                Row(new[]{"A · 방 만들기"},new UnityAction[]{()=>session.Host()},48);
                addressLabel=Label("",26,50,Color.white);
                Row(new[]{"1","2","3","4","5","6"},Enumerable.Range(1,6).Select(n=>(UnityAction)(()=>AddAddress(n.ToString()))).ToArray(),42);
                Row(new[]{"7","8","9","0",".","지움"},new UnityAction[]{()=>AddAddress("7"),()=>AddAddress("8"),()=>AddAddress("9"),()=>AddAddress("0"),()=>AddAddress("."),()=>{if(address.Length>0)address=address.Substring(0,address.Length-1);}},42);
                Row(new[]{"주소 지우기","B · 참가하기","연결 종료"},new UnityAction[]{()=>address="",()=>session.Join(address),()=>session.Leave()},48);
                if(!device) Row(new[]{"PC 테스트 주소: 127.0.0.1"},new UnityAction[]{()=>address="127.0.0.1"},42);
                Label("연결한 뒤 ③에서 실제 문 위치를 확인하세요.\n두 사람 모두 확인하고 준비해야 미션이 시작됩니다.",20,64,Color.white);
            }
            else if(index==1)
            {
                clue=Label("",21,75,new Color(.6f,.9f,1));
                missionState=Label("",20,50,Color.white);
                Row(new[]{"공간 확인 완료 · 준비"},new UnityAction[]{Ready},40);
                if(session.IsA || !session.Connected)
                {
                    codeLabel=Label("",25,30,Color.white);
                    Row(new[]{"0","1","2","3","4"},Enumerable.Range(0,5).Select(n=>(UnityAction)(()=>AddCode(n))).ToArray(),36);
                    Row(new[]{"5","6","7","8","9"},Enumerable.Range(5,5).Select(n=>(UnityAction)(()=>AddCode(n))).ToArray(),36);
                    Row(new[]{"암호 지우기","A · 암호 제출"},new UnityAction[]{()=>code="",()=>session.SubmitCode(code)},40);
                }
                else if(!device)
                {
                    Label("PC에서 B의 잡기·놓기 모의 입력",18,32,Color.white);
                    Row(new[]{"파란 열쇠 선택","빨간 열쇠 선택","받침대에 놓기"},new UnityAction[]{()=>selectedKey=1,()=>selectedKey=2,PlaceSelectedKey},42);
                }
                Label("Quest의 B: 열쇠를 그립으로 잡아 받침대에 놓으세요.\nA·B 모두 성공하면 문 영역이 공개됩니다.\n실제 문은 손으로 열고, 문 근처에서는 현실 전체를 봅니다.",18,72,Color.white);
                Row(new[]{"A · 처음부터 다시"},new UnityAction[]{()=>session.ResetRound()},38);
            }
            else
            {
                spaceState=Label("",21,80,Color.white);
                Label("현실 전체 보기 상태에서 확인하세요.\n흰 문틀이 실제 문·손잡이를 덮는지 확인합니다.\nQuest 공간 설정에 책상·문틀이 등록되어야 합니다.",21,96,Color.white);
                Row(new[]{"공간 다시 읽기","다음 문틀"},new UnityAction[]{()=>alignment.Reload(),()=>alignment.NextDoor()},48);
                Row(new[]{"폭 −5cm","폭 +5cm","높이 −5cm","높이 +5cm"},new UnityAction[]{()=>alignment.AdjustDoor(0,-.05f,0),()=>alignment.AdjustDoor(0,.05f,0),()=>alignment.AdjustDoor(0,0,-.05f),()=>alignment.AdjustDoor(0,0,.05f)},42);
                Row(new[]{"방향 −2°","방향 +2°"},new UnityAction[]{()=>alignment.AdjustDoor(-2,0,0),()=>alignment.AdjustDoor(2,0,0)},42);
                Row(new[]{"문·책상 위치 확인 완료"},new UnityAction[]{()=>alignment.Confirm()},50);
                Label("문이 맞지 않으면 진행하지 않고 공간 설정을 수정하세요.\nPC는 모의 방만 제공합니다.\nQuest의 B 버튼 또는 아래 버튼으로 현실 전체 보기가 가능합니다.",20,80,Color.white);
            }
            Refresh();
        }
        public void Ready()
        {
            if(alignment.Confirmed)
            {
                session.Ready();
                if(device && session.Connected && !session.IsA) panel.gameObject.SetActive(false);
            }
            else Show(2);
        }
        void AddAddress(string digit) { if(address.Length<15) address+=digit; }
        void AddCode(int digit) { if(code.Length<3) code+=digit; }
        public void PlaceSelectedKey()
        {
            if(session.IsA || !session.CanPlay) return;
            StartCoroutine(PlaceOnPc(selectedKey));
        }
        IEnumerator PlaceOnPc(int id)
        {
            yield return goal.ResetKeys(keys);
            if(!session.CanPlay) yield break;
            var key=keys.First(k=>k.keyId==id);
            // Same XR Socket listener used by the real grab-and-drop interaction.
            key.GetComponent<Rigidbody>().linearVelocity=Vector3.zero;
            key.GetComponent<Rigidbody>().position=goal.socket.attachTransform.position;
            key.transform.position=goal.socket.attachTransform.position;
            Physics.SyncTransforms();
        }
        void Update()
        {
            if(device && panel)
            {
                if(OVRInput.GetDown(OVRInput.Button.Three)) panel.gameObject.SetActive(!panel.gameObject.activeSelf);
                if(reveal.CrossingView&&!lastCrossing) panel.gameObject.SetActive(false);
                lastCrossing=reveal.CrossingView;
            }
            Refresh();
        }
        void Refresh()
        {
            if(!status) return;
            bool connected=session.Connected;
            string who=connected?(session.IsA?"A · 방장":"B · 참가자"):"연결 전";
            status.text=who+" | "+(connected&&session.PartnerPresent.Value?"2명 연결":"상대 대기")+"\n"+session.Notice;
            if(addressLabel) addressLabel.text="A의 주소: "+address;
            if(spaceState) spaceState.text=alignment.Status;
            if(codeLabel) codeLabel.text="A 암호: "+(code.Length==0?"___":code);
            if(clue) clue.text=connected?(session.IsA?"B에게 알려줄 힌트: 파란 열쇠가 정답입니다.\n내 미션: B에게 암호 세 자리를 물어보세요.":"A에게 알려줄 힌트: 암호는 3 → 1 → 4입니다.\n내 미션: A에게 맞는 열쇠 색을 물어보세요."):"먼저 연결하고 공간 위치를 확인하세요.";
            if(missionState) missionState.text=connected?$"준비 A: {(session.AReady.Value?"완료":"대기")} / B: {(session.BReady.Value?"완료":"대기")}\n미션 A: {(session.ASolved.Value?"성공":"대기")} / B: {(session.BSolved.Value?"성공":"대기")} · 문: {(reveal.IsOpen?"공개":"잠김")}":"미션 대기";
            if(connected&&lastRound!=session.Round.Value)
            {
                lastRound=session.Round.Value; code="";
                StartCoroutine(goal.ResetKeys(keys));
            }
        }
        TextMeshProUGUI Label(string text,int size,float height,Color color)
        {
            var label=new GameObject("Text",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            Layout(label.rectTransform,y,height); y+=height+10;
            label.font=font; label.fontSize=size; label.color=color; label.text=text; label.raycastTarget=false;
            label.textWrappingMode=TextWrappingModes.Normal;
            return label;
        }
        void Row(string[] names,UnityAction[] actions,float height)
        {
            float width=580f/names.Length;
            for(int n=0;n<names.Length;n++)
            {
                var button=new GameObject(names[n],typeof(RectTransform),typeof(Image),typeof(Button));
                var rt=button.GetComponent<RectTransform>(); Layout(rt,y,height);
                rt.sizeDelta=new Vector2(width-7,height); rt.anchoredPosition=new Vector2(-290+width*(n+.5f),-y);
                button.GetComponent<Image>().color=new Color(.1f,.24f,.32f);
                var b=button.GetComponent<Button>(); b.onClick.AddListener(actions[n]);
                var label=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
                label.transform.SetParent(button.transform,false); var l=label.rectTransform;l.anchorMin=Vector2.zero;l.anchorMax=Vector2.one;l.offsetMin=l.offsetMax=Vector2.zero;
                label.font=font; label.text=names[n];label.fontSize=names.Length>=4?17:20;label.alignment=TextAlignmentOptions.Center;label.raycastTarget=false;
            }
            y+=height+10;
        }
        void Layout(RectTransform rt,float top,float height)
        {
            rt.SetParent(body,false); rt.anchorMin=rt.anchorMax=new Vector2(.5f,1); rt.pivot=new Vector2(.5f,1);
            rt.anchoredPosition=new Vector2(0,-top);rt.sizeDelta=new Vector2(580,height);
        }
        void OnDestroy() { if(session) session.Changed-=Refresh; }
    }
}
