using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unrez.Networking;
using System;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;

namespace Unrez.BackyardShowdown
{
    public class InGameMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private InputReader _inputReader;
        [SerializeField]
        private GameObject _panelMain;
        [SerializeField]
        private Button _buttonLeaveMatch;
        [SerializeField]
        private Button _buttonSettings;
        [Header("SubMenus")]
        [SerializeField]
        private GameObject _panelSettings;
        private bool _inSettings;


        private bool _isVisible = false;

        private void Start()
        {
            _buttonLeaveMatch.onClick.AddListener(OnButtonLeaveMatch);
            _buttonSettings.onClick.AddListener(OnButtonSettings);
            _inputReader.OnToggleMenuEvent += OnToggleMenuHandler;
            ToggleMenu(false);
        }

        private void OnDestroy()
        {
            _buttonLeaveMatch.onClick.RemoveListener(OnButtonLeaveMatch);
            _buttonSettings.onClick.RemoveListener(OnButtonSettings);
            _inputReader.OnToggleMenuEvent -= OnToggleMenuHandler;
        }

        private void ToggleMenu(bool show)
        {
            _isVisible = show;
            _panelMain.SetActive(_isVisible);
        }

        private void OnToggleMenuHandler()
        {
            if (_inSettings)
            {
                _inSettings = false;
                _panelSettings.SetActive(_inSettings);
                return;
            }
            ToggleMenu(!_isVisible);
        }

        public void OnButtonLeaveMatch()
        {
            LeaveMatch();
        }

        public void OnButtonSettings()
        {
            _inSettings = true;
            _panelSettings.SetActive(_inSettings);
        }

        public async void LeaveMatch()
        {

            // Shutdown Netcode
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient)
            {
                Debug.Log("Shutting down the network...");
                NetworkManager.Singleton.Shutdown();
            }

            // If the player is the host, delete the lobby
            if (NetworkManager.Singleton.IsServer)
            {
                try
                {
                    Debug.Log("Deleting the lobby...");
                    await LobbyService.Instance.DeleteLobbyAsync(NetHandlerHost.Instance.GetLobby().Id);
                    Debug.Log("Lobby deleted successfully.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to delete the lobby: {ex}");
                }
            }
            else
            {
                try
                {
                    Debug.Log("Removing player from the lobby...");
                    await LobbyService.Instance.RemovePlayerAsync(NetHandlerClient.Instance.GetLobby().Id, AuthenticationService.Instance.PlayerId);
                    Debug.Log("Player removed from the lobby.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to leave the lobby: {ex}");
                }
            }

            // Clean up local state
            CleanupAfterLeave();

            // Optionally load the main menu or other scene
            LoadMainMenu();
        }

        private void CleanupAfterLeave()
        {
            Debug.Log("Cleaning up resources...");
        }

        private void LoadMainMenu()
        {
            Debug.Log("Loading main menu...");
            //NetworkManager.Singleton.SceneManager.LoadScene(NetworkingScenes.SCENE_MAIN_MENU, LoadSceneMode.Single);
            SceneManager.LoadScene(NetworkingScenes.SCENE_MAIN_MENU);
        }
    }
}
