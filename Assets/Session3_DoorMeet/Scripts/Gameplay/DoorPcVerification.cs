using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace Session3DoorMeet
{
    // Opt-in standalone checks; never runs during ordinary lessons.
    public sealed class DoorPcVerification : MonoBehaviour
    {
        public DoorSession session;
        public DoorMenu menu;
        public RoomAlignment alignment;
        public KeySocketGoal goal;
        public MissionKey[] keys;
        string mode;
        float started;
        float nextDiagnostic;
        void Start()
        {
            var args=Environment.GetCommandLineArgs(); int at=Array.IndexOf(args,"-dm-test");
            if(at<0||at+1>=args.Length) return;
            mode=args[at+1]; started=Time.realtimeSinceStartup;
            StartCoroutine(Run());
        }
        void Update()
        {
            if(mode!=null&&Time.realtimeSinceStartup>nextDiagnostic)
            {
                nextDiagnostic=Time.realtimeSinceStartup+5;
                Debug.Log($"[DoorMeet DIAG] {mode} connected={session.Connected} ready={session.AReady.Value}/{session.BReady.Value} solved={session.ASolved.Value}/{session.BSolved.Value} socket={(goal.socket.hasSelection?goal.socket.interactablesSelected[0].transform.name:"empty")} keys={string.Join(";",keys.Select(k=>k.name+":"+k.transform.position))}");
            }
            if(mode!=null && Time.realtimeSinceStartup-started>100) Fail("timeout "+mode);
        }
        IEnumerator Wait(Func<bool> predicate)
        {
            while(!predicate()) yield return null;
        }
        void Check(bool value,string name)
        {
            if(!value) Fail(name);
            Debug.Log("[DoorMeet TEST PASS] "+name);
        }
        void Fail(string name)
        {
            Debug.LogError("[DoorMeet TEST FAIL] "+name); Application.Quit(1); throw new InvalidOperationException(name);
        }
        IEnumerator Place(int id)
        {
            while(goal.IsResetting) yield return null;
            var key=keys.First(k=>k.keyId==id);
            key.GetComponent<Rigidbody>().position=goal.socket.attachTransform.position;
            key.transform.position=goal.socket.attachTransform.position;
            Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.6f);
            if(id!=1)
            {
                Check(goal.socket.hasSelection,"wrong key reaches the real XR socket");
                Check(!session.BSolved.Value,"wrong socket key rejected");
                yield return goal.ResetKeys(keys);
                Check(!goal.socket.hasSelection,"wrong key removed without reselection");
            }
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);
            alignment.Confirm();
            if(mode=="host")
            {
                Check(session.Host(),"start host");
                yield return Wait(()=>session.PartnerPresent.Value);
                session.SubmitCode("314"); yield return new WaitForSecondsRealtime(.3f);
                Check(!session.ASolved.Value&&!session.DoorUnlocked.Value,"cannot solve before both ready");
                menu.Ready(); yield return Wait(()=>session.BReady.Value);
                yield return new WaitForSecondsRealtime(1.5f);
                session.SubmitCode("123"); yield return new WaitForSecondsRealtime(.3f);
                Check(!session.ASolved.Value,"wrong code rejected");
                session.SubmitCode("314"); yield return Wait(()=>session.ASolved.Value);
                Check(!session.DoorUnlocked.Value,"one solution cannot unlock door");
                yield return Wait(()=>session.DoorUnlocked.Value);
                Check(session.BSolved.Value,"both solutions unlock door on host");
                yield return new WaitForSecondsRealtime(2);
                session.ResetRound(); yield return new WaitForSecondsRealtime(.5f);
                Check(!session.AReady.Value&&!session.BReady.Value&&!session.ASolved.Value&&!session.BSolved.Value&&!session.DoorUnlocked.Value,"reset clears ready solutions and door");
                Debug.Log("[DoorMeet TEST] RESET_COMPLETE");
                menu.Ready(); yield return Wait(()=>session.BReady.Value);
                session.SubmitCode("314"); yield return Wait(()=>session.DoorUnlocked.Value);
                Debug.Log("[DoorMeet TEST] SECOND_UNLOCKED");
                yield return Wait(()=>!session.PartnerPresent.Value);
                Check(!session.DoorUnlocked.Value&&!session.ASolved.Value&&!session.BSolved.Value,"disconnect revokes unlock and progress");
                yield return Wait(()=>session.PartnerPresent.Value);
                menu.Ready(); yield return Wait(()=>session.BReady.Value);
                session.SubmitCode("314"); yield return Wait(()=>session.DoorUnlocked.Value);
                Check(true,"rejoined peer starts fresh and completes new round");
                yield return new WaitForSecondsRealtime(1);
            }
            else if(mode=="client")
            {
                Check(session.Join("127.0.0.1"),"start client");
                yield return Wait(()=>session.Connected&&session.PartnerPresent.Value);
                menu.Ready(); yield return Wait(()=>session.CanPlay);
                session.SubmitCode("314"); yield return new WaitForSecondsRealtime(.3f);
                Check(!session.ASolved.Value,"B cannot complete A through public code API");
                yield return Place(2);
                yield return Wait(()=>session.ASolved.Value);
                yield return Place(1); yield return Wait(()=>session.DoorUnlocked.Value);
                Check(session.ASolved.Value&&session.BSolved.Value,"unlock state replicated to client");
                int round=session.Round.Value;
                yield return Wait(()=>session.Round.Value!=round);
                yield return new WaitForSecondsRealtime(1);
                Check(!session.ASolved.Value&&!session.BSolved.Value&&!session.DoorUnlocked.Value,"reset replicated to client");
                menu.Ready(); yield return Wait(()=>session.CanPlay);
                yield return Place(1); yield return Wait(()=>session.DoorUnlocked.Value);
                yield return new WaitForSecondsRealtime(8);
                session.Leave(); yield return new WaitForSecondsRealtime(2);
                Check(session.Join("127.0.0.1"),"reconnect client");
                yield return Wait(()=>session.Connected&&session.PartnerPresent.Value);
                yield return new WaitForSecondsRealtime(.5f);
                Check(!session.DoorUnlocked.Value&&!session.ASolved.Value&&!session.BSolved.Value,"reconnect receives fresh state");
                menu.Ready(); yield return Wait(()=>session.CanPlay);
                yield return Place(1); yield return Wait(()=>session.DoorUnlocked.Value);
                Check(true,"reconnected client completes socket mission");
                yield return new WaitForSecondsRealtime(.5f);
            }
            else if(mode=="reject")
            {
                Check(session.Join("127.0.0.1"),"third client attempts join");
                yield return new WaitForSecondsRealtime(3);
                Check(!session.Connected,"third participant rejected");
                Check(!string.IsNullOrEmpty(Unity.Netcode.NetworkManager.Singleton.DisconnectReason),"rejection reason supplied");
            }
            else if(mode=="visual")
            {
                session.Host(); yield return new WaitForSecondsRealtime(2);
                var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-dm-images");
                string output=at>=0&&at+1<args.Length?args[at+1]:Application.persistentDataPath;
                System.IO.Directory.CreateDirectory(output);
                foreach(var page in new[]{0,2,1})
                {
                    menu.OpenPage(page);yield return new WaitForSecondsRealtime(.5f);
                    var canvas=menu.GetComponentInChildren<Canvas>();var camera=Camera.main;
                    var previous=canvas.renderMode;var previousRect=camera.rect;camera.rect=new Rect(0,0,1,1);canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1f;
                    Canvas.ForceUpdateCanvases();yield return null;
                    var rt=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);rt.Create();
                    var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=rt};
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,request);
                    var before=RenderTexture.active;RenderTexture.active=rt;
                    var pixels=new Texture2D(1600,1000,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,1000),0,0);pixels.Apply();
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,"DoorMeet-PC-Page"+page+".png"),pixels.EncodeToPNG());
                    RenderTexture.active=before;canvas.renderMode=previous;camera.rect=previousRect;Destroy(pixels);rt.Release();Destroy(rt);
                    yield return new WaitForSecondsRealtime(.5f);
                }
            }
            else Fail("unknown test mode");
            Debug.Log("[DoorMeet TEST COMPLETE] "+mode);
            Application.Quit(0);
            mode=null;
        }
    }
}
