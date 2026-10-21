using System;
using System.IO;
using System.Linq;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Session2MR.Editor
{
    public static class MRLessonBuilder
    {
        const string Root="Assets/MR_TEST";
        public const string PreviewPath=Root+"/Scenes/MR_RoomPreview.unity";
        public const string LessonPath=Root+"/Scenes/MR_TableEvent.unity";
        [MenuItem("MR 실습/1. 공간 예시 열기")]
        public static void OpenPreview(){EnsureScenes();EditorSceneManager.OpenScene(PreviewPath);}
        [MenuItem("MR 실습/2. 책상과 이벤트 열기")]
        public static void OpenLesson(){EnsureScenes();EditorSceneManager.OpenScene(LessonPath);}
        [MenuItem("MR 실습/4. Quest 공간·패스스루 설정 준비")]
        public static void ConfigureQuest()
        {
            var config=OVRProjectConfig.CachedProjectConfig;
            config.sceneSupport=OVRProjectConfig.FeatureSupport.Supported;
            config.insightPassthroughSupport=OVRProjectConfig.FeatureSupport.Supported;
            OVRProjectConfig.CommitProjectConfig(config);AssetDatabase.SaveAssets();
            Debug.Log("[MR LESSON] Scene and Passthrough support enabled. Check Meta Project Setup Tool before a Quest build.");
        }
        public static void EnsureScenes()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            foreach(var folder in new[]{"Scenes","Materials","Prefabs","Fonts","Settings","Documentation"})Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();
            if(!File.Exists(PreviewPath))Create(true);
            if(!File.Exists(LessonPath))Create(false);
            var list=EditorBuildSettings.scenes.ToList();
            foreach(var path in new[]{PreviewPath,LessonPath})if(!list.Any(s=>s.path==path))list.Add(new EditorBuildSettingsScene(path,false));
            EditorBuildSettings.scenes=list.ToArray();AssetDatabase.SaveAssets();
        }
        static void Create(bool preview)
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var teal=Material("Platform",new Color(.08f,.55f,.56f));
            var wood=Material("Mock Table",new Color(.46f,.32f,.19f));
            var dark=Material("Room",new Color(.15f,.21f,.27f));
            var pale=Material("Goal",new Color(.85f,.95f,.94f));
            var greenMat=Material("Green Box",new Color(.1f,.72f,.3f));
            var redMat=Material("Red Box",new Color(.82f,.2f,.2f));
            var doorMat=Material("Door",new Color(.85f,.53f,.16f));
            var manager=new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            var mock=new GameObject("01_PC Mock Room - NOT real room recognition");
            Cube("Floor",mock.transform,new Vector3(0,-.07f,0),new Vector3(5,.14f,5),dark);
            Cube("Mock Table 1.60 x 0.90m",mock.transform,new Vector3(0,.71f,0),new Vector3(1.6f,.08f,.9f),wood);
            foreach(float x in new[]{-.65f,.65f})foreach(float z in new[]{-.32f,.32f})Cube("Table Leg",mock.transform,new Vector3(x,.34f,z),new Vector3(.07f,.68f,.07f),wood);
            var content=new GameObject("02_Table Matched Content").transform;
            var platform=Cube("Platform - table size",content,new Vector3(0,.023f,0),new Vector3(1.6f,.04f,.9f),teal);
            var goal=Cube("White Target",content,new Vector3(.45f,.055f,0),new Vector3(.32f,.05f,.32f),pale);
            var green=Prop("Green Box - Correct",content,greenMat,true,manager);
            var red=Prop("Red Box - Wrong",content,redMat,false,manager);
            var doorRoot=new GameObject("03_Virtual Door - not the physical door").transform;doorRoot.position=new Vector3(.4f,0,1.5f);
            Cube("Frame L",doorRoot,new Vector3(-.43f,.85f,0),new Vector3(.06f,1.7f,.09f),pale);
            Cube("Frame R",doorRoot,new Vector3(.43f,.85f,0),new Vector3(.06f,1.7f,.09f),pale);
            Cube("Frame Top",doorRoot,new Vector3(0,1.73f,0),new Vector3(.92f,.08f,.09f),pale);
            var hinge=new GameObject("Door Hinge").transform;hinge.SetParent(doorRoot,false);hinge.localPosition=new Vector3(-.4f,0,0);
            var slab=Cube("Door Slab",hinge,new Vector3(.4f,.85f,0),new Vector3(.79f,1.67f,.06f),doorMat);
            UnityEngine.Object.DestroyImmediate(slab.GetComponent<Collider>());
            Cube("Handle",hinge,new Vector3(.7f,.85f,-.06f),new Vector3(.06f,.06f,.1f),pale);
            var desktop=new GameObject("04_PC Camera");var camera=desktop.AddComponent<Camera>();desktop.AddComponent<AudioListener>();
            camera.tag="MainCamera";camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.1f,.14f);camera.fieldOfView=50;
            camera.rect=new Rect(.33f,0,.67f,1);camera.GetUniversalAdditionalCameraData().allowXRRendering=false;
            desktop.transform.position=new Vector3(2.2f,2.15f,-2.9f);desktop.transform.LookAt(new Vector3(0,.85f,.35f));
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
            var quest=(GameObject)PrefabUtility.InstantiatePrefab(prefab);quest.name="05_Quest Rig - device only";
            var rig=quest.GetComponent<OVRCameraRig>();var ovr=quest.GetComponent<OVRManager>();
            ovr.isInsightPassthroughEnabled=true;ovr.trackingOriginType=OVRManager.TrackingOrigin.FloorLevel;
            var managerSettings=new SerializedObject(ovr);managerSettings.FindProperty("requestScenePermissionOnStartup").boolValue=true;managerSettings.ApplyModifiedPropertiesWithoutUndo();
            var passthrough=quest.AddComponent<OVRPassthroughLayer>();passthrough.textureOpacity=1;
            var questCamera=rig.centerEyeAnchor.GetComponent<Camera>();questCamera.tag="MainCamera";questCamera.clearFlags=CameraClearFlags.SolidColor;questCamera.backgroundColor=new Color(0,0,0,0);
            questCamera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var inputPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
            if(!inputPrefab)throw new Exception("XRI Starter Assets 3.5.1 prefab missing.");
            var input=(GameObject)PrefabUtility.InstantiatePrefab(inputPrefab);input.name="XRI Controllers - no locomotion";input.transform.SetParent(rig.trackingSpace,false);
            foreach(var cam in input.GetComponentsInChildren<Camera>(true)){cam.enabled=false;cam.tag="Untagged";foreach(var audio in cam.GetComponents<AudioListener>())UnityEngine.Object.DestroyImmediate(audio);}
            foreach(var tr in input.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(tr.gameObject);
            var origin=input.GetComponent<Unity.XR.CoreUtils.XROrigin>();origin.CameraYOffset=0;origin.RequestedTrackingOriginMode=Unity.XR.CoreUtils.XROrigin.TrackingOriginMode.NotSpecified;origin.CameraFloorOffsetObject.transform.localPosition=Vector3.zero;
            var locomotion=input.transform.Find("Locomotion");if(locomotion)locomotion.gameObject.SetActive(false);
            var room=new GameObject("MRUK - Device scene data");room.transform.SetParent(quest.transform,false);
            var mruk=room.AddComponent<MRUK>();mruk.EnableWorldLock=true;mruk.SceneSettings=new MRUK.MRUKSettings{LoadSceneOnStartup=true,DataSource=MRUK.SceneDataSource.Device};
            var effect=room.AddComponent<EffectMesh>();effect.MeshMaterial=teal;effect.Labels=MRUKAnchor.SceneLabels.TABLE|MRUKAnchor.SceneLabels.WALL_FACE|MRUKAnchor.SceneLabels.FLOOR;effect.Colliders=!preview;effect.HideMesh=!preview;
            var officePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.meta.xr.mrutilitykit/Core/Rooms/Prefabs/Office/Office00.prefab");
            var office=(GameObject)PrefabUtility.InstantiatePrefab(officePrefab);office.name="06_Official MRUK Office00 - PC preview only";
            if(preview)
            {
                var b=new Bounds();bool any=false;
                foreach(var r in office.GetComponentsInChildren<Renderer>()){if(!any){b=r.bounds;any=true;}else b.Encapsulate(r.bounds);}
                desktop.transform.position=b.center+new Vector3(b.extents.x+2,b.extents.y+2,-b.extents.z-2);
                desktop.transform.LookAt(b.center);
            }
            var system=new GameObject("07_MR Lesson Systems");var lesson=system.AddComponent<MRTableLesson>();
            lesson.previewOnly=preview;lesson.content=content;lesson.platform=platform.transform;lesson.goal=goal.transform;lesson.doorHinge=hinge;
            lesson.desktopRig=desktop;lesson.questRig=quest;lesson.mockRoom=mock;lesson.previewRoom=office;lesson.mruk=mruk;
            lesson.green=green;lesson.red=red;lesson.pcCamera=camera;
            lesson.guiFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/NotoSansCJKkr-Regular.otf");
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"/Fonts/MRLessonKorean.asset");
            if(!font)
            {
                font=TMP_FontAsset.CreateFontAsset(lesson.guiFont,32,4,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);font.name="MR Lesson Korean";font.isMultiAtlasTexturesEnabled=true;
                AssetDatabase.CreateAsset(font,Root+"/Fonts/MRLessonKorean.asset");AssetDatabase.AddObjectToAsset(font.material,font);foreach(var atlas in font.atlasTextures)AssetDatabase.AddObjectToAsset(atlas,font);
            }
            var canvasObj=new GameObject("Quest Status - prepared UI");canvasObj.transform.SetParent(rig.centerEyeAnchor,false);canvasObj.transform.localPosition=new Vector3(0,.35f,1.6f);canvasObj.transform.localScale=Vector3.one*.0015f;
            var canvas=canvasObj.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.GetComponent<RectTransform>().sizeDelta=new Vector2(900,230);
            var textObj=new GameObject("Instructions");textObj.transform.SetParent(canvasObj.transform,false);var text=textObj.AddComponent<TextMeshProUGUI>();text.font=font;text.fontSize=23;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.text="MR · 책상 맞춤과 이벤트";text.rectTransform.sizeDelta=new Vector2(900,230);lesson.questText=text;
            var source=GraphicsSettings.defaultRenderPipeline;string path=Root+"/Settings/MRLessonURP.asset";
            if(source && !File.Exists(path))AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source),path);
            var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(path);
            if(pipeline){var settings=new SerializedObject(pipeline);var alpha=settings.FindProperty("m_AllowPostProcessAlphaOutput");if(alpha!=null)alpha.boolValue=true;var hdr=settings.FindProperty("m_SupportsHDR");if(hdr!=null)hdr.boolValue=false;settings.ApplyModifiedPropertiesWithoutUndo();lesson.pipeline=pipeline;}
            quest.SetActive(false);office.SetActive(preview);mock.SetActive(!preview);content.gameObject.SetActive(false);
            if(preview)doorRoot.gameObject.SetActive(false);
            var test=system.AddComponent<MRPcVerification>();test.lesson=lesson;
            var light=new GameObject("Directional Light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.transform.rotation=Quaternion.Euler(50,-35,0);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.62f,.65f,.7f);
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,preview?PreviewPath:LessonPath);
        }
        static Material Material(string name,Color color)
        {
            var path=Root+"/Materials/"+name+".mat";var found=AssetDatabase.LoadAssetAtPath<Material>(path);if(found)return found;
            var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));mat.color=color;AssetDatabase.CreateAsset(mat,path);return mat;
        }
        static GameObject Cube(string name,Transform parent,Vector3 position,Vector3 size,Material material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;return obj;
        }
        static MRProp Prop(string name,Transform parent,Material mat,bool correct,XRInteractionManager manager)
        {
            var obj=Cube(name,parent,Vector3.zero,Vector3.one*.16f,mat);var body=obj.AddComponent<Rigidbody>();body.mass=.35f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.interpolation=RigidbodyInterpolation.Interpolate;
            var grab=obj.AddComponent<XRGrabInteractable>();grab.interactionManager=manager;grab.movementType=XRBaseInteractable.MovementType.VelocityTracking;grab.throwOnDetach=false;
            var prop=obj.AddComponent<MRProp>();prop.body=body;prop.grab=grab;prop.correct=correct;return prop;
        }
        [MenuItem("MR 실습/3. PC 연습 프로그램 만들기")]
        public static void BuildPc()
        {
            EnsureScenes();EditorSceneManager.OpenScene(LessonPath);
            var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/MR_PC"));Directory.CreateDirectory(folder);
            var xr=UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            bool init=xr && xr.InitManagerOnStart;var loaders=xr?xr.Manager.activeLoaders.ToList():null;
            var target=UnityEditor.Build.NamedBuildTarget.Standalone;var backend=PlayerSettings.GetScriptingBackend(target);var preload=PlayerSettings.GetPreloadedAssets();
            try
            {
                if(xr){xr.InitManagerOnStart=false;xr.Manager.TrySetLoaders(new System.Collections.Generic.List<UnityEngine.XR.Management.XRLoader>());}
                PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.Mono2x);PlayerSettings.SetPreloadedAssets(Array.Empty<UnityEngine.Object>());
                var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{LessonPath,PreviewPath},locationPathName=folder+"/MR_Practice.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
                if(result.summary.result!=BuildResult.Succeeded)throw new Exception("MR PC build failed: "+result.summary.result);
                Debug.Log("[MR LESSON] PC build ready "+folder);
            }
            finally{if(xr){xr.Manager.TrySetLoaders(loaders);xr.InitManagerOnStart=init;}PlayerSettings.SetScriptingBackend(target,backend);PlayerSettings.SetPreloadedAssets(preload);AssetDatabase.SaveAssets();}
        }
        public static void PolishAndBuild()
        {
            EnsureScenes();EditorSceneManager.OpenScene(PreviewPath);
            var lesson=UnityEngine.Object.FindFirstObjectByType<MRTableLesson>();
            foreach(var renderer in lesson.previewRoom.GetComponentsInChildren<Renderer>(true))
            {
                var node=renderer.transform;
                bool ceiling=false;
                while(node && node!=lesson.previewRoom.transform){if(node.name=="CEILING")ceiling=true;node=node.parent;}
                if(ceiling)renderer.enabled=false;
            }
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene(LessonPath);
            var main=UnityEngine.Object.FindFirstObjectByType<MRTableLesson>();main.questRig.GetComponentInChildren<EffectMesh>(true).Colliders=true;
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            BuildPc();
        }
        [MenuItem("MR 실습/5. 실습 패키지 내보내기")]
        public static void ExportLesson()
        {
            EnsureScenes();
            var pipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(Root+"/Settings/MRLessonURP.asset");
            if(pipeline)
            {
                var settings=new SerializedObject(pipeline);var renderers=settings.FindProperty("m_RendererDataList");
                for(int i=0;i<renderers.arraySize;i++)
                {
                    var item=renderers.GetArrayElementAtIndex(i);var source=item.objectReferenceValue;
                    if(!source)continue;
                    var sourcePath=AssetDatabase.GetAssetPath(source);if(sourcePath.StartsWith(Root+"/"))continue;
                    var target=Root+"/Settings/MRRenderer"+i+".asset";
                    if(!File.Exists(target))AssetDatabase.CopyAsset(sourcePath,target);
                    item.objectReferenceValue=AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(target);
                }
                settings.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
            }
            var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/MR-TablePractice.unitypackage"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var roots=AssetDatabase.FindAssets("",new[]{Root}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var assets=AssetDatabase.GetDependencies(roots,true).Where(p=>p.StartsWith("Assets/",StringComparison.Ordinal)).Distinct().ToArray();
            AssetDatabase.ExportPackage(assets,path,ExportPackageOptions.Default);
            Debug.Log("[MR LESSON] Package exported "+path);
        }
    }
    [InitializeOnLoad]
    public static class MRPlayPreview
    {
        const string Key="Session2MR.PreviousXRInit";
        static MRPlayPreview(){EditorApplication.playModeStateChanged+=Change;}
        static void Change(PlayModeStateChange state)
        {
            var xr=UnityEditor.XR.Management.XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);if(!xr)return;
            if(state==PlayModeStateChange.ExitingEditMode && UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.StartsWith("Assets/MR_TEST/"))
            {SessionState.SetInt(Key,xr.InitManagerOnStart?1:0);xr.InitManagerOnStart=false;EditorUtility.SetDirty(xr);AssetDatabase.SaveAssets();}
            if(state==PlayModeStateChange.EnteredEditMode)
            {int old=SessionState.GetInt(Key,-1);if(old<0)return;xr.InitManagerOnStart=old==1;SessionState.EraseInt(Key);EditorUtility.SetDirty(xr);AssetDatabase.SaveAssets();}
        }
    }
}
