using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

namespace Session2MR
{
    public sealed class MRPcVerification : MonoBehaviour
    {
        public MRTableLesson lesson;
        readonly List<string> results=new List<string>();
        string output;
        void Check(bool condition,string name)
        {
            results.Add((condition?"PASS ":"FAIL ")+name);
            if(!condition)throw new Exception("MR test failed: "+name);
            Debug.Log("[MR TEST PASS] "+name);
        }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();
            if(args.Contains("-mr-room")){if(!lesson.previewOnly)UnityEngine.SceneManagement.SceneManager.LoadScene(1);yield break;}
            bool test=args.Contains("-mr-test");bool preview=args.Contains("-mr-preview");
            int idx=Array.IndexOf(args,"-mr-output");output=idx>=0?args[idx+1]:Application.persistentDataPath;
            if(!test && !preview)yield break;
            Directory.CreateDirectory(output);
            if(preview && !lesson.previewOnly)
            {UnityEngine.SceneManagement.SceneManager.LoadScene(1);yield break;}
            yield return new WaitForSeconds(.4f);
            if(preview)
            {
                yield return new WaitForEndOfFrame();CaptureScene("MR-01-RoomPreview.png");
                yield return new WaitForSeconds(.5f);Application.Quit(0);yield break;
            }
            try { LogicalTests(); }
            catch(Exception e){File.WriteAllText(Path.Combine(output,"MR-test-result.txt"),string.Join("\n",results)+"\n"+e);Debug.LogException(e);Application.Quit(1);yield break;}
            lesson.FitMock();
            yield return new WaitForSeconds(.4f);
            yield return new WaitForEndOfFrame();CaptureScene("MR-02-TableMatched.png");
            lesson.PlaceMock(lesson.red);
            yield return new WaitForSeconds(.75f);
            try { Check(lesson.State==LessonState.Ready,"wrong physical box keeps door closed: "+lesson.State+" green="+lesson.green.transform.position+" parent="+(lesson.green.transform.parent?lesson.green.transform.parent.name:"none")); }
            catch(Exception e){File.WriteAllText(Path.Combine(output,"MR-test-result.txt"),string.Join("\n",results)+"\n"+e);Application.Quit(1);yield break;}
            lesson.red.Restore();lesson.PlaceMock(lesson.green);
            yield return new WaitForSeconds(1.8f);
            try
            {
                Check(lesson.State==LessonState.Completed,"physics placement completes the event");
                Check(lesson.CompletionCount==1,"event fired exactly once");
                Check(Quaternion.Angle(lesson.doorHinge.localRotation,Quaternion.identity)>90,"door animation opens beyond ninety degrees");
            }
            catch(Exception e){File.WriteAllText(Path.Combine(output,"MR-test-result.txt"),string.Join("\n",results)+"\n"+e);Debug.LogException(e);Application.Quit(1);yield break;}
            yield return new WaitForEndOfFrame();CaptureScene("MR-03-DoorOpened.png");
            yield return new WaitForSeconds(.5f);
            lesson.ResetLesson();
            Check(lesson.State==LessonState.Ready && lesson.CompletionCount==0 && Quaternion.Angle(lesson.doorHinge.localRotation,Quaternion.identity)<.1f,"reset restores ready state and closes door");
            File.WriteAllText(Path.Combine(output,"MR-test-result.txt"),"Unity PC simulation verification\n"+string.Join("\n",results)+"\nAndroid/Quest spatial scan, passthrough and controller grip: NOT hardware-tested.\n");
            Application.Quit(0);
        }
        void CaptureScene(string name)
        {
            var camera=lesson.pcCamera;var oldRect=camera.rect;
            var overlay=new GameObject("PC scene-render caption");overlay.transform.SetParent(camera.transform,false);
            overlay.transform.localPosition=new Vector3(0,.55f,1.5f);overlay.transform.localScale=Vector3.one*.001f;
            var canvas=overlay.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.GetComponent<RectTransform>().sizeDelta=new Vector2(1300,190);
            var labelObject=new GameObject("Caption");labelObject.transform.SetParent(overlay.transform,false);
            var text=labelObject.AddComponent<TextMeshProUGUI>();text.font=lesson.questText.font;text.fontSize=24;text.alignment=TextAlignmentOptions.Center;text.color=Color.white;
            text.rectTransform.sizeDelta=new Vector2(1300,190);text.text="PC 모의 씬 렌더링 · 실제 Quest 화면 아님\n"+lesson.Status;text.ForceMeshUpdate();
            var render=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);render.Create();camera.rect=new Rect(0,0,1,1);
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=render};RenderPipeline.SubmitRenderRequest(camera,request);
            var oldTarget=RenderTexture.active;RenderTexture.active=render;
            var texture=new Texture2D(1600,1000,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,1000),0,0);texture.Apply();
            File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());
            File.WriteAllText(Path.Combine(output,"MR-capture-info.txt"),"PC simulation scenes rendered by Unity's scene camera into a render texture. Not Quest passthrough or native-window screenshots.\n");
            RenderTexture.active=oldTarget;camera.rect=oldRect;render.Release();Destroy(render);Destroy(texture);Destroy(overlay);
        }
        void LogicalTests()
        {
            Check(!lesson.DeviceMode && lesson.State==LessonState.Waiting && !lesson.content.gameObject.activeSelf,"PC starts without claiming real room sensing");
            Check(!lesson.questRig.activeSelf,"Quest camera and room scanner remain disabled on PC");
            lesson.Evaluate(1);Check(lesson.CompletionCount==0,"no completion before valid table fit");
            var matrix=Matrix4x4.TRS(new Vector3(2,.75f,-1),Quaternion.Euler(-90,37,0),Vector3.one);
            Check(MRTableGeometry.FromPlane(matrix,new Rect(-.8f,-.45f,1.6f,.9f),out var plane),"horizontal plane geometry accepted");
            Check(Vector3.Distance(plane.center,new Vector3(2,.75f,-1))<.001f && Vector2.Distance(plane.size,new Vector2(1.6f,.9f))<.001f,"plane center and footprint preserve scan dimensions");
            Check(MRTableGeometry.FromVolume(matrix,new Bounds(new Vector3(0,0,-.375f),new Vector3(1.6f,.9f,.75f)),out var volume),"volume table geometry accepted");
            Check(Vector3.Distance(volume.center,plane.center)<.001f && Vector2.Distance(volume.size,plane.size)<.001f,"volume top agrees with tabletop plane");
            Check(!MRTableGeometry.FromPlane(Matrix4x4.identity,new Rect(0,0,1,1),out _),"vertical plane rejected as tabletop");
            Check(!MRTableGeometry.FromPlane(matrix,new Rect(0,0,0,.9f),out _),"zero width surface rejected");
            var scaled=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(-90,0,0),new Vector3(2,3,1));
            Check(MRTableGeometry.FromPlane(scaled,new Rect(0,0,1,.5f),out var scaledPose) && Vector2.Distance(scaledPose.size,new Vector2(2,1.5f))<.001f,"transform scale is applied once");
            var tilted=Matrix4x4.TRS(Vector3.zero,Quaternion.Euler(-85,20,0),Vector3.one);
            Check(MRTableGeometry.FromPlane(tilted,new Rect(0,0,1,.5f),out var tiltedPose) && Vector3.Angle(tiltedPose.rotation*Vector3.up,tilted.MultiplyVector(Vector3.forward))<.01f,"small tabletop tilt is preserved");
            lesson.Fit(new TablePose{center=Vector3.zero,rotation=Quaternion.identity,size=new Vector2(.35f,.25f)});
            Check(lesson.goal.localPosition.x+lesson.goal.localScale.x*.5f<.35f*.5f,"target stays on a small valid tabletop");
            Check(lesson.Fit(plane) && Vector3.Distance(lesson.content.position,plane.center)<.001f && Quaternion.Angle(lesson.content.rotation,plane.rotation)<.1f,"content aligns with position and yaw");
            var expectedDoor=plane.center+(plane.rotation*Vector3.forward)*(plane.size.y*.5f+.85f);expectedDoor.y=0;
            Check(Vector3.Distance(lesson.doorHinge.parent.position,expectedDoor)<.001f,"virtual door is positioned relative to the fitted table");
            Check(Mathf.Abs(lesson.platform.localScale.x-1.6f)<.001f && Mathf.Abs(lesson.platform.localScale.z-.9f)<.001f,"platform uses same table width and depth");
            lesson.FitMock();lesson.green.body.useGravity=false;lesson.red.body.useGravity=false;
            lesson.PlaceMock(lesson.red);lesson.Evaluate(1);
            Check(!lesson.MeetsGoal(lesson.red) && lesson.State==LessonState.Ready,"wrong identity cannot complete condition");
            lesson.red.Restore();lesson.PlaceMock(lesson.green);
            var pos=lesson.green.transform.position;
            lesson.green.PlaceWorld(pos+lesson.content.up*(.08f*(Mathf.Sqrt(2)-1)),lesson.content.rotation*Quaternion.Euler(0,0,45));
            Check(lesson.MeetsGoal(lesson.green),"rotated box uses actual collider bottom and footprint");
            lesson.PlaceMock(lesson.green);
            lesson.green.transform.position=pos+Vector3.up*.12f;lesson.Evaluate(1);
            Check(lesson.State==LessonState.Ready,"floating box above the surface is rejected");
            lesson.green.transform.position=pos+lesson.content.right*.25f;lesson.Evaluate(1);
            Check(lesson.State==LessonState.Ready,"outside target area rejected");
            lesson.green.transform.position=pos;
            var testObject=new GameObject("temporary test interactor");var sphere=testObject.AddComponent<SphereCollider>();sphere.isTrigger=true;
            var interactor=testObject.AddComponent<XRDirectInteractor>();var manager=lesson.green.grab.interactionManager;interactor.interactionManager=manager;
            manager.SelectEnter((IXRSelectInteractor)interactor,(IXRSelectInteractable)lesson.green.grab);
            Check(lesson.green.Held && !lesson.MeetsGoal(lesson.green),"held box does not complete placement");
            lesson.ResetLesson();Check(!lesson.green.Held,"reset cancels active grab before moving props");
            Destroy(testObject);lesson.PlaceMock(lesson.green);lesson.Evaluate(.2f);
            lesson.green.transform.position+=Vector3.right*.3f;lesson.Evaluate(.1f);
            Check(lesson.StableTime==0,"leaving target resets dwell timer");
            lesson.PlaceMock(lesson.green);int events=0;lesson.onCompleted.AddListener(()=>events++);
            lesson.Evaluate(.49f);Check(lesson.State==LessonState.Ready,"short dwell does not solve");
            lesson.Evaluate(.02f);lesson.Evaluate(1);
            Check(lesson.State==LessonState.Completed && events==1 && lesson.CompletionCount==1,"stable placement fires a single completion event");
            lesson.green.body.useGravity=true;lesson.red.body.useGravity=true;
            lesson.FitMock();
        }
    }
}
