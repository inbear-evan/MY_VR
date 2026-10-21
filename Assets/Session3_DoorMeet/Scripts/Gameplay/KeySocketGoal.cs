using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace Session3DoorMeet
{
    public sealed class KeySocketGoal : MonoBehaviour
    {
        public DoorSession session;
        public XRSocketInteractor socket;
        public bool IsResetting { get; private set; }
        bool wasPlayable;
        public IEnumerator ResetKeys(MissionKey[] keys)
        {
            if(IsResetting) { while(IsResetting) yield return null; yield break; }
            IsResetting=true;
            // Let physics remove old trigger contacts before registering the grab objects again.
            socket.socketActive=false;
            foreach(var key in keys) key.ResetKey(false);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            foreach(var key in keys) key.ResumeGrab();
            socket.socketActive=true;
            yield return new WaitForSecondsRealtime(socket.recycleDelayTime+.1f);
            IsResetting=false;
        }
        public void CheckPlacedKey()
        {
            if (!socket.hasSelection) return;
            var key = socket.GetOldestInteractableSelected().transform.GetComponent<MissionKey>();
            Debug.Log($"[DoorMeet Socket] selected={key?.name} key={key?.keyId} playable={session.CanPlay} host={session.IsA}");
            if (key) session.SubmitKey(key.keyId);
        }
        void Start() { socket.selectEntered.AddListener(_ => CheckPlacedKey()); }
        void Update()
        {
            bool playable=session.CanPlay;
            if(playable&&!wasPlayable) CheckPlacedKey();
            wasPlayable=playable;
        }
    }
}
