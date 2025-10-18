using System.Collections.Generic;
using System;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using UnityEngine.SceneManagement;
using UnityEngine;
using Unrez.Essential;
using System.Threading.Tasks;
using System.Collections;

namespace Unrez.Networking
{
    public class NetHandlerHost : Singleton<NetHandlerHost>
    {
        private Allocation _allocation;
        private string _joinCode;
        private string _connectionType;
        private string _roomName;
        private string _userName;
        private int _minPlayers;
        private int _maxPlayers;

        private Lobby _lobby;

        public void Initialize(string connectionType, int minPlayers, int maxPlayers, string roomName = "RoomName", string userName = "HostName")
        {
            _connectionType = connectionType;
            _roomName = roomName;
            _userName = userName;
            _minPlayers = minPlayers;
            _maxPlayers = maxPlayers;
        }

        public async Task StartHostAsync()
        {
            try
            {
                _allocation = await RelayService.Instance.CreateAllocationAsync(_maxPlayers);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Can NOT CreateAllocationAsync. ERROR: {ex}");
                return;
            }
            //JoinCode
            try
            {
                _joinCode = await RelayService.Instance.GetJoinCodeAsync(_allocation.AllocationId);
                Debug.Log($"<color=magenta>Allocation created with JoinCode:{_joinCode}</color>");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Can NOT GetJoinCodeAsync. ERROR: {ex}");
                return;
            }

            //Transport
            UnityTransport unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            RelayServerData relayServerData = AllocationUtils.ToRelayServerData(_allocation, _connectionType);
            unityTransport.SetRelayServerData(relayServerData);

            //Lobby
            try
            {
                CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions();
                createLobbyOptions.IsPrivate = false;
                createLobbyOptions.Data = new Dictionary<string, DataObject>()
                {
                    {
                        "JoinCode", new DataObject(visibility:DataObject.VisibilityOptions.Member, value:_joinCode)
                    }
                };
                _lobby = await LobbyService.Instance.CreateLobbyAsync(_userName, _maxPlayers, createLobbyOptions);
                StartCoroutine(HeartbeatLobby(15));
            }
            catch (LobbyServiceException lobbyException)
            {
                Debug.Log($"Fail to create the lobby. ERROR: {lobbyException}");
            }

            NetworkManager.Singleton.StartHost();
            NetworkManager.Singleton.SceneManager.LoadScene(NetworkingScenes.SCENE_MATCH_NAME, LoadSceneMode.Single);
        }

        public Lobby GetLobby()
        {
            return _lobby;
        }

        private IEnumerator HeartbeatLobby(float heartbeat)
        {
            WaitForSecondsRealtime delay = new WaitForSecondsRealtime(heartbeat);
            while (true)
            {
                LobbyService.Instance.SendHeartbeatPingAsync(_lobby.Id);
                yield return delay;
            }
        }
    }
}