using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Session2.Editor
{
    [InitializeOnLoad]
    public static class Session2EditorBridge
    {
        static readonly string Folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/Session2"));
        static double next;
        static Session2EditorBridge() { EditorApplication.update+=Poll; }
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<next) return;next=EditorApplication.timeSinceStartup+1;
            Directory.CreateDirectory(Folder);
            var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            File.WriteAllText(Path.Combine(Folder,"state.txt"),"playing="+EditorApplication.isPlaying+"\ncompiling="+EditorApplication.isCompiling+"\nscene="+current.path+"\ndirty="+current.isDirty+"\ntime="+DateTime.UtcNow.ToString("O"));
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlaying) return;
            string command=Path.Combine(Folder,"command.txt");if(!File.Exists(command)) return;
            string value=File.ReadAllText(command).Trim();File.Delete(command);
            try
            {
                var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Session2.Editor.Session2SceneBuilder")).FirstOrDefault(t=>t!=null);
                if(type==null) throw new InvalidOperationException("Session2SceneBuilder is not compiled yet.");
                var method=type.GetMethod(value,System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
                if(method==null) throw new InvalidOperationException("Unknown command "+value);
                method.Invoke(null,null);
                File.WriteAllText(Path.Combine(Folder,"result.txt"),"OK "+value+" "+DateTime.UtcNow.ToString("O"));
            }
            catch(Exception e) {Debug.LogException(e);File.WriteAllText(Path.Combine(Folder,"result.txt"),"ERROR "+e);}
        }
    }
}
