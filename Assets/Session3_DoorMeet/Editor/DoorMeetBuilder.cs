using System;
using System.IO;
using System.Linq;
using Meta.XR.MRUtilityKit;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Session3DoorMeet.Editor
{
    public static class DoorMeetBuilder
    {
        const string Root="Assets/Session3_DoorMeet";
        public const string ScenePath=Root+"/Scenes/DoorMeet.unity";
        [MenuItem("Session 3 Door Meet/1. Open or Create Scene")]
        public static void CreateScene()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode first.");
            if(File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            foreach(var dir in new[]{"Scenes","Materials","Prefabs","Settings","Fonts","Documentation"}) Directory.CreateDirectory(Root+"/"+dir);
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var teal=Mat("Teal",new Color(.04f,.65f,.65f));
            var wall=Mat("Virtual room",new Color(.13f,.18f,.24f));
            var floor=Mat("Floor",new Color(.3f,.35f,.42f));
            var blue=Mat("Blue key",new Color(.03f,.3f,1));
            var red=Mat("Red key",new Color(.95f,.14f,.18f));
            var gold=Mat("Door locked",new Color(.7f,.4f,.08f));
            var white=Mat("Frame",new Color(.85f,.95f,1));
            var windowMat=new Material(Shader.Find("Session3/Door Passthrough Window"));
            windowMat.SetFloat("_Opacity",0); AssetDatabase.CreateAsset(windowMat,Root+"/Materials/DoorWindow.mat");
            var demo=new GameObject("01_PC_MockRoom");
            Cube("Floor",demo.transform,new Vector3(0,-.08f,0),new Vector3(6,.16f,6),floor);
            Cube("LeftWall",demo.transform,new Vector3(-3,1.5f,0),new Vector3(.1f,3,6),wall);
            Cube("RightWall",demo.transform,new Vector3(3,1.5f,0),new Vector3(.1f,3,6),wall);
            Cube("BackWallLeft",demo.transform,new Vector3(-1.85f,1.5f,3),new Vector3(2.3f,3,.1f),wall);
            Cube("BackWallRight",demo.transform,new Vector3(1.85f,1.5f,3),new Vector3(2.3f,3,.1f),wall);
            Cube("DoorLintel",demo.transform,new Vector3(0,2.65f,3),new Vector3(1.4f,.7f,.1f),wall);
            Cube("MockTable",demo.transform,new Vector3(0,.72f,.6f),new Vector3(1.8f,.12f,.8f),teal);
            foreach(float x in new[]{-.75f,.75f}) foreach(float z in new[]{.3f,.9f}) Cube("Leg",demo.transform,new Vector3(x,.35f,z),new Vector3(.07f,.7f,.07f),teal);
            var door=new GameObject("02_LocalDoorAlignment");door.transform.position=new Vector3(0,1.1f,2.91f);door.transform.localScale=new Vector3(1.2f,2.2f,1);
            var opaque=Cube("VirtualDoorLocked",door.transform,Vector3.zero,new Vector3(1,1,.06f),gold);
            var window=GameObject.CreatePrimitive(PrimitiveType.Quad);window.name="RealDoorPassthroughWindow";window.transform.SetParent(door.transform,false);window.GetComponent<Renderer>().sharedMaterial=windowMat; UnityEngine.Object.DestroyImmediate(window.GetComponent<Collider>());window.SetActive(false);
            Cube("FrameLeft",door.transform,new Vector3(-.53f,0,0),new Vector3(.035f,1.08f,.04f),white);
            Cube("FrameRight",door.transform,new Vector3(.53f,0,0),new Vector3(.035f,1.08f,.04f),white);
            Cube("FrameTop",door.transform,new Vector3(0,.53f,0),new Vector3(1.08f,.035f,.04f),white);
            var partner=new GameObject("PC_PartnerSymbol");partner.transform.position=new Vector3(0,0,3.4f);
            var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.transform.SetParent(partner.transform,false);body.transform.localPosition=new Vector3(0,.9f,0);body.transform.localScale=new Vector3(.5f,.85f,.5f);body.GetComponent<Renderer>().sharedMaterial=teal;partner.SetActive(false);
            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            var network=new GameObject("03_LAN_NetworkManager");var transport=network.AddComponent<UnityTransport>();var manager=network.AddComponent<NetworkManager>();
            manager.NetworkConfig.NetworkTransport=transport;manager.NetworkConfig.ConnectionApproval=true;manager.NetworkConfig.EnableSceneManagement=true;
            var state=new GameObject("04_SharedPuzzleState");state.AddComponent<NetworkObject>();var session=state.AddComponent<DoorSession>();
            var mission=new GameObject("05_LocalMissionProps");mission.transform.position=new Vector3(0,.81f,.6f);
            var keys=new[]{Key("BlueKey",1,new Vector3(-.5f,.13f,0),mission.transform,blue),Key("RedKey",2,new Vector3(-.1f,.13f,0),mission.transform,red)};
            var socketObject=new GameObject("KeySocket_B");socketObject.transform.SetParent(mission.transform,false);socketObject.transform.localPosition=new Vector3(.55f,.13f,0);
            var collider=socketObject.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=.16f;
            var socket=socketObject.AddComponent<XRSocketInteractor>();socket.showInteractableHoverMeshes=false;
            var attach=new GameObject("AttachPoint").transform;attach.SetParent(socketObject.transform,false);socket.attachTransform=attach;
            Cube("SocketStand",mission.transform,new Vector3(.55f,.01f,0),new Vector3(.32f,.04f,.32f),white);
            var goal=socketObject.AddComponent<KeySocketGoal>();goal.socket=socket;goal.session=session;
            var desktop=new GameObject("06_DesktopView");var pcCamera=desktop.AddComponent<Camera>();desktop.AddComponent<AudioListener>();pcCamera.tag="MainCamera";pcCamera.clearFlags=CameraClearFlags.SolidColor;pcCamera.backgroundColor=new Color(.02f,.03f,.05f);pcCamera.fieldOfView=52;
            desktop.transform.position=new Vector3(2.4f,2.2f,-4);desktop.transform.LookAt(new Vector3(0,1,1.8f));pcCamera.rect=new Rect(.4f,0,.6f,1);pcCamera.GetUniversalAdditionalCameraData().allowXRRendering=false;
            var rigPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
            if(!rigPrefab) throw new Exception("Meta camera rig prefab missing.");
            var quest=(GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);quest.name="07_QuestRig_AndRoomScan";quest.SetActive(false);
            var ovr=quest.GetComponent<OVRCameraRig>();var ovrManager=quest.GetComponent<OVRManager>();ovrManager.isInsightPassthroughEnabled=true;ovrManager.trackingOriginType=OVRManager.TrackingOrigin.FloorLevel;
            var layer=quest.AddComponent<OVRPassthroughLayer>();layer.textureOpacity=1;
            var headCamera=ovr.centerEyeAnchor.GetComponent<Camera>();headCamera.tag="MainCamera";headCamera.clearFlags=CameraClearFlags.SolidColor;headCamera.backgroundColor=new Color(0,0,0,0);
            headCamera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var xriPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
            var xri=(GameObject)PrefabUtility.InstantiatePrefab(xriPrefab);xri.name="XRI_Controllers_NoLocomotion";xri.transform.SetParent(ovr.trackingSpace,false);
            foreach(var cam in xri.GetComponentsInChildren<Camera>(true)) {cam.enabled=false;cam.tag="Untagged";foreach(var audio in cam.GetComponents<AudioListener>()) UnityEngine.Object.DestroyImmediate(audio);}
            var origin=xri.GetComponent<Unity.XR.CoreUtils.XROrigin>();origin.CameraYOffset=0;origin.RequestedTrackingOriginMode=Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.NotSpecified;origin.CameraFloorOffsetObject.transform.localPosition=Vector3.zero;
            var locomotion=xri.transform.Find("Locomotion");if(locomotion)locomotion.gameObject.SetActive(false);
            var roomObject=new GameObject("RoomScan_MRUK");roomObject.transform.SetParent(quest.transform,false);var mruk=roomObject.AddComponent<MRUK>();mruk.EnableWorldLock=true;mruk.SceneSettings=new MRUK.MRUKSettings{LoadSceneOnStartup=true,DataSource=MRUK.SceneDataSource.Device};
            var effect=roomObject.AddComponent<EffectMesh>();effect.MeshMaterial=wall;effect.Labels=(MRUKAnchor.SceneLabels)~0;effect.Colliders=true;effect.CutHoles=0;
            var systems=new GameObject("08_DoorMeet_LocalSystems");var reveal=systems.AddComponent<DoorReveal>();var alignment=systems.AddComponent<RoomAlignment>();var platform=systems.AddComponent<DoorPlatform>();var menu=systems.AddComponent<DoorMenu>();
            reveal.session=session;reveal.doorRoot=door.transform;reveal.opaqueDoor=opaque;reveal.window=window;reveal.demoRoom=demo;reveal.missionRoot=mission;reveal.pcPartner=partner;reveal.roomMesh=effect;reveal.passthrough=layer;reveal.deviceCamera=headCamera;
            alignment.doorRoot=door.transform;alignment.missionRoot=mission.transform;alignment.viewCamera=headCamera;alignment.mruk=mruk;alignment.roomMesh=effect;alignment.reveal=reveal;alignment.keys=keys;alignment.demoRoom=demo;alignment.session=session;
            platform.questRig=quest;platform.desktopRig=desktop;platform.reveal=reveal;
            var pipelinePath=Root+"/Settings/DoorMeetURP.asset";
            var source=GraphicsSettings.defaultRenderPipeline;
            if(source) { AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source),pipelinePath);var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pipelinePath);var so=new SerializedObject(pipeline);so.FindProperty("m_AllowPostProcessAlphaOutput").boolValue=true;so.FindProperty("m_SupportsHDR").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();platform.scenePipeline=pipeline; }
            var fontSource=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansCJKkr-Regular.otf");
            var font=TMP_FontAsset.CreateFontAsset(fontSource,32,4,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);font.name="DoorMeet Korean";font.isMultiAtlasTexturesEnabled=true;
            // Prewarm only lesson characters so headsets do not rasterize a whole CJK font.
            string chars=string.Concat(Directory.GetFiles(Root+"/Scripts","*.cs",SearchOption.AllDirectories).Select(File.ReadAllText));font.TryAddCharacters(new string(chars.Distinct().ToArray()));
            AssetDatabase.CreateAsset(font,Root+"/Fonts/DoorMeetKorean.asset");AssetDatabase.AddObjectToAsset(font.material,font);foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
            menu.session=session;menu.alignment=alignment;menu.reveal=reveal;menu.goal=goal;menu.keys=keys;menu.font=font;menu.questCamera=headCamera;
            var tests=systems.AddComponent<DoorPcVerification>();tests.session=session;tests.menu=menu;tests.alignment=alignment;tests.goal=goal;tests.keys=keys;
            var events=new GameObject("EventSystem");events.AddComponent<EventSystem>();events.AddComponent<XRUIInputModule>();
            var sun=new GameObject("Directional Light").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.7f;sun.transform.rotation=Quaternion.Euler(50,-30,0);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.55f,.6f,.7f);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
            // Do not change which earlier lesson scene the project builds by default.
            if(!EditorBuildSettings.scenes.Any(s=>s.path==ScenePath)) EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(ScenePath,false)}).ToArray();
            Debug.Log("[DoorMeet] New scene created: "+ScenePath);
        }
        static Material Mat(string name,Color color)
        { var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.color=color;AssetDatabase.CreateAsset(m,Root+"/Materials/"+name+".mat");return m; }
        static GameObject Cube(string name,Transform parent,Vector3 p,Vector3 scale,Material material)
        { var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;return g; }
        static MissionKey Key(string name,int id,Vector3 position,Transform parent,Material mat)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=Vector3.one*.16f;obj.GetComponent<Renderer>().sharedMaterial=mat;
            var rb=obj.AddComponent<Rigidbody>();rb.mass=.1f;
            var grab=obj.AddComponent<XRGrabInteractable>();grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.throwOnDetach=false;
            var key=obj.AddComponent<MissionKey>();key.keyId=id;
            PrefabUtility.SaveAsPrefabAssetAndConnect(obj,Root+"/Prefabs/"+name+".prefab",InteractionMode.AutomatedAction);
            return key;
        }
        [MenuItem("Session 3 Door Meet/2. Configure Quest Features (no build)")]
        public static void ConfigureQuest()
        {
            var config=OVRProjectConfig.CachedProjectConfig;
            config.sceneSupport=OVRProjectConfig.FeatureSupport.Required;
            config.insightPassthroughSupport=OVRProjectConfig.FeatureSupport.Required;
            OVRProjectConfig.CommitProjectConfig(config);
            var settings=UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            var feature=settings.GetFeature<Meta.XR.MetaXRFeature>();if(feature)feature.enabled=true;
            EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
            Debug.Log("[DoorMeet] Quest scene/passthrough and Meta OpenXR features configured. No APK built.");
        }
        [MenuItem("Session 3 Door Meet/3. Build Windows PC Verification")]
        public static void BuildPc()
        {
            CreateScene();
            string outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/Session3_DoorMeet_PC"));Directory.CreateDirectory(outDir);
            // OpenXR's post-build copier does not replace its own previously generated file.
            string bindings=Path.Combine(outDir,"RuntimeActionBindings.json");
            if(File.Exists(bindings)) File.Delete(bindings);
            var xr=UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            bool original=xr&&xr.InitManagerOnStart;
            var originalLoaders=xr?xr.Manager.activeLoaders.ToList():null;
            var target=UnityEditor.Build.NamedBuildTarget.Standalone;
            var originalBackend=PlayerSettings.GetScriptingBackend(target);
            var originalPreloads=PlayerSettings.GetPreloadedAssets();
            try
            {
                if(xr) { xr.InitManagerOnStart=false;xr.Manager.TrySetLoaders(new System.Collections.Generic.List<UnityEngine.XR.Management.XRLoader>()); }
                PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.Mono2x);
                PlayerSettings.SetPreloadedAssets(Array.Empty<UnityEngine.Object>());
                var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=outDir+"/DoorMeet.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                if(result.summary.result!=BuildResult.Succeeded) throw new Exception("PC build failed: "+result.summary.result);
                Debug.Log("[DoorMeet] PC build ready: "+outDir);
            }
            finally
            {
                if(xr) { xr.Manager.TrySetLoaders(originalLoaders);xr.InitManagerOnStart=original; }
                PlayerSettings.SetScriptingBackend(target,originalBackend);
                PlayerSettings.SetPreloadedAssets(originalPreloads);
                AssetDatabase.SaveAssets();
            }
        }
        public static void PolishPcViewAndBuild()
        {
            CreateScene();
            var platform=UnityEngine.Object.FindFirstObjectByType<DoorPlatform>();
            var camera=platform.desktopRig.GetComponent<Camera>();
            camera.transform.position=new Vector3(2.4f,2.2f,-4);
            camera.transform.LookAt(new Vector3(0,1,1.8f));
            camera.rect=new Rect(.4f,0,.6f,1);
            camera.GetUniversalAdditionalCameraData().allowXRRendering=false;
            foreach(var node in platform.questRig.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(node.gameObject);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            BuildPc();
        }
    }
}
