using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Session3DoorMeet
{
    // Only puzzle state crosses the network. Each headset owns its real-room alignment.
    public sealed class DoorSession : NetworkBehaviour
    {
        public const ushort Port = 7779;
        public NetworkVariable<bool> AReady = new(false);
        public NetworkVariable<bool> BReady = new(false);
        public NetworkVariable<bool> ASolved = new(false);
        public NetworkVariable<bool> BSolved = new(false);
        public NetworkVariable<bool> DoorUnlocked = new(false);
        public NetworkVariable<bool> PartnerPresent = new(false);
        public NetworkVariable<int> Round = new(0);
        public NetworkVariable<ulong> BClientId = new(ulong.MaxValue);
        public event Action Changed;
        public string Notice { get; private set; } = "방을 만들거나 참가하세요.";
        public bool IsA => NetworkManager && NetworkManager.IsHost;
        public bool Connected => IsSpawned && NetworkManager.IsConnectedClient;
        public bool CanPlay => Connected && PartnerPresent.Value && AReady.Value && BReady.Value;
        public bool LocalReady => IsA ? AReady.Value : BReady.Value;
        public bool LocalSolved => IsA ? ASolved.Value : BSolved.Value;
        public ulong RemoteId => BClientId.Value;
        bool configured;
        bool approvingGuest;
        ulong reservedGuest=ulong.MaxValue;

        public void Configure()
        {
            if (configured) return;
            configured = true;
            var manager = Unity.Netcode.NetworkManager.Singleton;
            manager.NetworkConfig.ConnectionApproval = true;
            manager.NetworkConfig.ConnectionData = System.Text.Encoding.UTF8.GetBytes("DoorMeet-v1");
            manager.ConnectionApprovalCallback = Approval;
            manager.OnClientConnectedCallback += ClientConnected;
            manager.OnClientDisconnectCallback += ClientDisconnected;
            manager.OnTransportFailure += TransportFailed;
        }
        void Approval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            bool host = request.ClientNetworkId == Unity.Netcode.NetworkManager.ServerClientId;
            bool valid = System.Text.Encoding.UTF8.GetString(request.Payload) == "DoorMeet-v1";
            bool roomFree = !approvingGuest && !PartnerPresent.Value;
            response.Approved = host || (valid && roomFree);
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = valid ? "이 방은 두 사람만 참가할 수 있습니다." : "프로젝트 버전이 다릅니다.";
            if (response.Approved && !host) { approvingGuest = true; reservedGuest=request.ClientNetworkId; }
        }
        public bool Host()
        {
            Configure();
            var manager = Unity.Netcode.NetworkManager.Singleton;
            if (manager.IsListening) return false;
            manager.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", Port, "0.0.0.0");
            Notice = "방을 만들었습니다. B의 참가를 기다립니다.";
            bool started = manager.StartHost();
            if (!started) Notice = "방을 만들지 못했습니다. 포트 사용 여부를 확인하세요.";
            Changed?.Invoke();
            return started;
        }
        public bool Join(string address)
        {
            Configure();
            var manager = Unity.Netcode.NetworkManager.Singleton;
            if (manager.IsListening) return false;
            if (!System.Net.IPAddress.TryParse(address.Trim(), out var ip) ||
                ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            { Notice = "호스트의 IPv4 주소를 입력하세요."; Changed?.Invoke(); return false; }
            manager.GetComponent<UnityTransport>().SetConnectionData(address.Trim(), Port);
            Notice = "연결 중입니다…";
            bool started = manager.StartClient();
            if (!started) Notice = "연결을 시작하지 못했습니다.";
            Changed?.Invoke();
            return started;
        }
        public void Leave()
        {
            Unity.Netcode.NetworkManager.Singleton.Shutdown();
            approvingGuest = false;
            Notice = "연결을 종료했습니다. 다시 참가할 수 있습니다.";
            Changed?.Invoke();
        }
        void TransportFailed() { Notice = "네트워크 오류: 같은 Wi-Fi와 방화벽을 확인하세요."; Changed?.Invoke(); }
        void ClientConnected(ulong id)
        {
            if (IsServer && id != Unity.Netcode.NetworkManager.ServerClientId)
            {
                approvingGuest = false; reservedGuest=ulong.MaxValue;
                BClientId.Value = id;
                PartnerPresent.Value = true;
                ClearRound();
            }
            Notice = "연결되었습니다. 두 사람 모두 공간을 확인하고 준비하세요.";
            Changed?.Invoke();
        }
        void ClientDisconnected(ulong id)
        {
            if (IsServer && id != Unity.Netcode.NetworkManager.ServerClientId)
            {
                if(id==reservedGuest) { approvingGuest=false; reservedGuest=ulong.MaxValue; }
                if (id == BClientId.Value)
                { PartnerPresent.Value = false; BClientId.Value = ulong.MaxValue; ClearRound(); Notice = "상대 연결이 끊겼습니다. 공개 상태는 해제됩니다."; }
            }
            if (!IsServer || id == BClientId.Value) Notice = "상대 연결이 끊겼습니다. 공개 상태는 해제됩니다.";
            if (!IsServer)
            {
                var reason = Unity.Netcode.NetworkManager.Singleton.DisconnectReason;
                Notice = string.IsNullOrWhiteSpace(reason) ? "연결이 끊겼습니다. 주소·Wi-Fi를 확인하세요." : reason;
            }
            Changed?.Invoke();
        }
        public override void OnNetworkSpawn()
        {
            AReady.OnValueChanged += BoolChanged; BReady.OnValueChanged += BoolChanged;
            ASolved.OnValueChanged += BoolChanged; BSolved.OnValueChanged += BoolChanged;
            DoorUnlocked.OnValueChanged += BoolChanged; PartnerPresent.OnValueChanged += BoolChanged;
            Round.OnValueChanged += RoundChanged;
            Changed?.Invoke();
        }
        public override void OnNetworkDespawn()
        {
            AReady.OnValueChanged -= BoolChanged; BReady.OnValueChanged -= BoolChanged;
            ASolved.OnValueChanged -= BoolChanged; BSolved.OnValueChanged -= BoolChanged;
            DoorUnlocked.OnValueChanged -= BoolChanged; PartnerPresent.OnValueChanged -= BoolChanged;
            Round.OnValueChanged -= RoundChanged;
            Changed?.Invoke();
        }
        void BoolChanged(bool before, bool after) => Changed?.Invoke();
        void RoundChanged(int before, int after) => Changed?.Invoke();
        bool ValidSender(ulong id) => id == Unity.Netcode.NetworkManager.ServerClientId ||
            (PartnerPresent.Value && id == BClientId.Value);
        public void Ready() { if (Connected) SetReadyRpc(true); }
        public void RevokeReady() { if(Connected) SetReadyRpc(false); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void SetReadyRpc(bool ready, RpcParams p = default)
        {
            if (!ValidSender(p.Receive.SenderClientId) || !PartnerPresent.Value) return;
            if (p.Receive.SenderClientId == Unity.Netcode.NetworkManager.ServerClientId)
            { AReady.Value = ready; if(!ready) ASolved.Value=false; }
            else { BReady.Value = ready; if(!ready) BSolved.Value=false; }
            if(!ready) DoorUnlocked.Value=false;
        }
        public void SubmitCode(string code)
        {
            if (!CanPlay || !IsA) { Notice = "A의 차례이며, 두 사람의 준비가 필요합니다."; Changed?.Invoke(); return; }
            if (code != "314") Notice = "암호가 다릅니다. B가 가진 힌트를 확인하세요.";
            else { Notice = "A 미션 성공!"; SubmitSolutionRpc(314); }
            Changed?.Invoke();
        }
        public void SubmitKey(int keyId)
        {
            if (!CanPlay || IsA) { Notice = "B의 차례이며, 두 사람의 준비가 필요합니다."; Changed?.Invoke(); return; }
            if (keyId != 1) Notice = "다른 열쇠입니다. A의 힌트를 확인하세요.";
            else { Notice = "B 미션 성공!"; SubmitSolutionRpc(1); }
            Changed?.Invoke();
        }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void SubmitSolutionRpc(int answer, RpcParams p = default)
        {
            if (!PartnerPresent.Value || !AReady.Value || !BReady.Value || !ValidSender(p.Receive.SenderClientId)) return;
            if (p.Receive.SenderClientId == Unity.Netcode.NetworkManager.ServerClientId)
            { if (answer == 314) ASolved.Value = true; }
            else { if (answer == 1) BSolved.Value = true; }
            DoorUnlocked.Value = ASolved.Value && BSolved.Value;
        }
        public void ResetRound() { if (Connected && IsA) ResetRoundRpc(); }
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        void ResetRoundRpc(RpcParams p = default)
        { if (p.Receive.SenderClientId == Unity.Netcode.NetworkManager.ServerClientId) ClearRound(); }
        void ClearRound()
        {
            DoorUnlocked.Value = false; ASolved.Value = false; BSolved.Value = false;
            AReady.Value = false; BReady.Value = false; Round.Value++;
        }
        public override void OnDestroy()
        {
            var manager = Unity.Netcode.NetworkManager.Singleton;
            if (configured && manager)
            {
                manager.OnClientConnectedCallback -= ClientConnected;
                manager.OnClientDisconnectCallback -= ClientDisconnected;
                manager.OnTransportFailure -= TransportFailed;
            }
            base.OnDestroy();
        }
    }
}
