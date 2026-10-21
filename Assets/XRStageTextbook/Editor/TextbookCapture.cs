using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

[InitializeOnLoad]
public static class TextbookCapture
{
    const string Root = "Assets/XRStageTextbook";
    const string ScenePath = Root + "/StageDecorating.unity";
    static string CommandPath => Path.Combine(Application.dataPath, "../Temp/TextbookCaptureCommand.txt");
    static double nextPoll;
    static double releaseAt;
    static TextbookCapture() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if(releaseAt>0 && EditorApplication.timeSinceStartup>=releaseAt) { if(Keyboard.current!=null) InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState()); releaseAt=0; }
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup < nextPoll) return;
        nextPoll = EditorApplication.timeSinceStartup + .5;
        if (!File.Exists(CommandPath)) return;
        var cmd = File.ReadAllText(CommandPath).Trim(); File.Delete(CommandPath);
        try {
            if (cmd == "build") Build();
            else if (cmd == "settings") SettingsService.OpenProjectSettings("Project/XR Plug-in Management/XR Interaction Toolkit");
            else if (cmd == "samples") UnityEditor.PackageManager.UI.Window.Open("com.unity.xr.interaction.toolkit");
            else if (cmd == "scenes") { Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ScenePath); EditorGUIUtility.PingObject(Selection.activeObject); }
            else if (cmd == "prefab") { Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Prop_Box.prefab"); EditorGUIUtility.PingObject(Selection.activeObject); }
            else if (cmd == "overview") { Selection.activeObject = null; var v = SceneView.lastActiveSceneView; if(v) { v.LookAt(new Vector3(-.7f,.5f,1.4f), Quaternion.Euler(30,0,0), 10f); v.Repaint(); } }
            else if (cmd == "status") Status();
            else if (cmd == "fixteleport") { foreach(var area in UnityEngine.Object.FindObjectsByType<TeleportationArea>(FindObjectsSortMode.None)) { area.interactionLayers = new InteractionLayerMask { value = unchecked((int)0x80000000) }; area.colliders.Clear(); area.colliders.Add(area.GetComponent<Collider>()); EditorUtility.SetDirty(area); } EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene()); }
            else if (cmd.StartsWith("input:")) { var parts=cmd.Split(':'); var keys=parts[1].Split(',').Select(k=>(Key)Enum.Parse(typeof(Key),k,true)).ToArray(); InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(keys)); releaseAt=EditorApplication.timeSinceStartup+double.Parse(parts[2],System.Globalization.CultureInfo.InvariantCulture); }
            else if (cmd.StartsWith("select:")) { var go = GameObject.Find(cmd.Substring(7)); Selection.activeGameObject = go; if(go) EditorGUIUtility.PingObject(go); }
        } catch (Exception e) { Debug.LogException(e); }
    }
    static Material Mat(string name, Color color)
    {
        var path = Root + "/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color=color; AssetDatabase.CreateAsset(mat,path); }
        return mat;
    }
    static GameObject Cube(string name, Transform parent, Vector3 p, Vector3 scale, Material m)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=p; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=m; return go;
    }
    [MenuItem("XR Textbook/Create Stage Exercise")]
    public static void Build()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode first.");
        if(File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
        var sample=Sample.FindByPackage("com.unity.xr.interaction.toolkit", "3.5.1").First(s=>s.displayName=="XR Interaction Simulator");
        if(!sample.isImported) { sample.Import(); File.WriteAllText(CommandPath,"build"); return; }
        Directory.CreateDirectory(Root+"/Materials"); Directory.CreateDirectory(Root+"/Prefabs"); AssetDatabase.Refresh();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var wood=Mat("Wood",new Color(.57f,.34f,.18f)); var floor=Mat("Floor",new Color(.65f,.69f,.72f)); var wall=Mat("Backdrop",new Color(.24f,.28f,.32f)); var teal=Mat("Teal",new Color(.1f,.6f,.57f));
        var environment=new GameObject("Environment");
        var ground=Cube("Ground",environment.transform,new Vector3(0,-.1f,0),new Vector3(12,.2f,12),floor);
        ground.AddComponent<TeleportationArea>();
        var stage=Cube("Stage",environment.transform,new Vector3(0,.15f,2),new Vector3(6,.3f,4),wood); stage.AddComponent<TeleportationArea>();
        Cube("BackWall",environment.transform,new Vector3(0,1.8f,4.1f),new Vector3(6,3,.2f),wall);
        Cube("PreparationZone",environment.transform,new Vector3(-4,.01f,0),new Vector3(2,.02f,3),teal);
        new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
        var rigPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
        var rig=(GameObject)PrefabUtility.InstantiatePrefab(rigPrefab); rig.transform.position=new Vector3(0,0,-3);
        var camera=rig.GetComponentInChildren<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.12f,.16f,.2f);
        var events=new GameObject("EventSystem"); events.AddComponent<EventSystem>(); events.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>();
        var light=new GameObject("Directional Light").AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1.8f; light.transform.rotation=Quaternion.Euler(45,-30,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=new Color(.65f,.65f,.65f);
        var props=new GameObject("Props");
        MakeProp("Prop_Box",new Vector3(-4,.03f,-1),props.transform,wood,0);
        MakeProp("Prop_Chair",new Vector3(-4,.03f,0),props.transform,wood,1);
        MakeProp("Prop_Table",new Vector3(-4,.03f,1),props.transform,wood,2);
        MakeProp("Prop_Decoration",new Vector3(-3.3f,.03f,-1),props.transform,teal,0);
        var socket=new GameObject("PlacementSocket"); socket.transform.position=new Vector3(1,.3f,2); var trigger=socket.AddComponent<BoxCollider>(); trigger.isTrigger=true; trigger.size=new Vector3(.7f,.7f,.7f); trigger.center=new Vector3(0,.35f,0);
        var socketAttach=new GameObject("SocketAttach").transform; socketAttach.SetParent(socket.transform,false); socketAttach.localPosition=new Vector3(0,.25f,0);
        var interactor=socket.AddComponent<XRSocketInteractor>(); interactor.attachTransform=socketAttach;
        Cube("PlacementMarker",environment.transform,new Vector3(1,.305f,2),new Vector3(.7f,.01f,.7f),teal);
        EnableSimulator(); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene,ScenePath); Selection.activeGameObject=stage;
        if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(-.7f,.5f,1.4f),Quaternion.Euler(30,0,0),10f);
        Debug.Log("XR Textbook: StageDecorating created; original scenes preserved."); Status();
    }
    static void MakeProp(string name,Vector3 p,Transform parent,Material mat,int kind)
    {
        var root=new GameObject(name); root.transform.SetParent(parent,false); root.transform.position=p;
        if(kind==0) Cube("Visual",root.transform,new Vector3(0,.25f,0),Vector3.one*.5f,mat);
        if(kind==1) {
            Cube("Seat",root.transform,new Vector3(0,.45f,0),new Vector3(.5f,.1f,.5f),mat);
            Cube("Back",root.transform,new Vector3(0,.8f,.22f),new Vector3(.5f,.6f,.08f),mat);
            foreach(float x in new[]{-.2f,.2f}) foreach(float z in new[]{-.2f,.2f}) Cube("Leg",root.transform,new Vector3(x,.2f,z),new Vector3(.06f,.4f,.06f),mat);
        }
        if(kind==2) {
            Cube("Top",root.transform,new Vector3(0,.75f,0),new Vector3(1.2f,.1f,.7f),mat);
            foreach(float x in new[]{-.5f,.5f}) foreach(float z in new[]{-.25f,.25f}) Cube("Leg",root.transform,new Vector3(x,.35f,z),new Vector3(.08f,.7f,.08f),mat);
        }
        var attach=new GameObject("GrabPoint").transform; attach.SetParent(root.transform,false); attach.localPosition=new Vector3(0,kind==2?.75f:kind==1?.5f:.25f,0);
        var rb=root.AddComponent<Rigidbody>(); rb.mass=kind==2?2:1;
        var grab=root.AddComponent<XRGrabInteractable>(); grab.attachTransform=attach; grab.movementType=XRBaseInteractable.MovementType.Kinematic; grab.throwOnDetach=false;
        var prefab=PrefabUtility.SaveAsPrefabAssetAndConnect(root,Root+"/Prefabs/"+name+".prefab",InteractionMode.AutomatedAction);
    }
    static void EnableSimulator()
    {
        var t=typeof(UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator).Assembly.GetType("UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulatorSettings");
        var prop=t.GetProperty("Instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy);
        var settings=(ScriptableObject)prop.GetValue(null); var so=new SerializedObject(settings);
        so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue=true;
        so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue=true;
        so.FindProperty("m_UseClassic").boolValue=false;
        so.FindProperty("m_SimulatorPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/XR Interaction Simulator/XR Interaction Simulator.prefab");
        so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(settings);
    }
    static void Status()
    {
        var data="time="+DateTime.Now.ToString("o")+"\nplay="+EditorApplication.isPlaying+"\nscene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+"\n";
        var sim=UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRInteractionSimulator>(); if(sim) data+="sim="+EditorJsonUtility.ToJson(sim)+"\n";
        foreach(var go in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.StartsWith("Prop_")||t.name=="XR Origin (XR Rig)"||t.name=="Main Camera")) {
            var g=go.GetComponent<XRGrabInteractable>(); data+=go.name+" position="+go.position.ToString("F3")+" rotation="+go.eulerAngles.ToString("F1")+(g?" selected="+g.isSelected+" by="+string.Join(",",g.interactorsSelecting.Select(i=>i.transform.name)):"")+"\n";
        }
        File.WriteAllText(Path.Combine(Application.dataPath,"../Temp/TextbookCaptureStatus.txt"),data);
    }
}
