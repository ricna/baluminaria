using System;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unrez.Essential;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;

namespace Unrez.Networking
{
    public class NetHandlerClient : Singleton<NetHandlerClient>
    {
        private JoinAllocation _joinAllocation;
        private string _userName;
        private string _connectionType;
        private Lobby _lobby;
        public void Initialize(string connectionType, string userName = "ClientUserName")
        {
            _connectionType = connectionType;
            _userName = userName;
        }

        public async Task JoinGameAsync(string joinCode)
        {
            try
            {
                _joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                Debug.Log($"<color=cyan>JoinGame with code:{joinCode}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Can NOT JoinAllocationAsync. ERROR: {ex}");
                return;
            }
            UnityTransport unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(_joinAllocation, _connectionType);
            unityTransport.SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }

        public void SetLobby(Lobby lobby)
        {
            _lobby = lobby;
        }

        public Lobby GetLobby()
        {
            return _lobby;
        }

        public int GetNextId()
        {
            return _lobby.Players.Count - 1;
        }
    }
}