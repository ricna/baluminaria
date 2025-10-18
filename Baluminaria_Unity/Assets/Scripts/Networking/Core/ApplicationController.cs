using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unrez.Essential;

namespace Unrez.Networking
{
    public enum ConnectionType
    {
        UDP,
        DTLS,
    }

    public enum Transport
    {
        Unity,
        Steam,
    }

    public class ApplicationController : Singleton<ApplicationController>
    {
        //Protocol DTLS is the recommended. Is a better version of UDP, but may not work in some computers yet. Change to 'udp' if needed.
        public const string CONNECTION_TYPE_DTLS = "dtls";
        public const string CONNECTION_TYPE_UDP = "udp";
        public const int MIN_PLAYERS = 2;
        public const int MAX_PLAYERS = 5;

        public ConnectionType _connectionType;
        public string _connectionTypeString;
        private bool _isDedicatedServer;

        public bool IsAuthenticated { get; private set; }

        private void Awake()
        {
            if (Instance)
            {
                Debug.Log("ApplicationController.Instance is ready");
            }
            switch (_connectionType)
            {
                case ConnectionType.UDP:
                    _connectionTypeString = CONNECTION_TYPE_UDP;
                    break;
                case ConnectionType.DTLS:
                    _connectionTypeString = CONNECTION_TYPE_DTLS;
                    break;
            }
        }

        private async void Start()
        {
            IsAuthenticated = false;
            _isDedicatedServer = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
            await LaunchAsync();
        }

        private async Task LaunchAsync()
        {
            if (_isDedicatedServer)
            {
                //ServerSingleton.Instance.
            }
            else
            {
                NetHandlerHost.Instance.Initialize(_connectionTypeString, MIN_PLAYERS, MAX_PLAYERS, "RoomName", "HostName");
                NetHandlerClient.Instance.Initialize(_connectionTypeString, "MyClientName");
                bool authenticated = await InitAsync();
                if (authenticated)
                {
                    IsAuthenticated = true;
                    GoToMenu();
                }
            }
        }

        public async Task<bool> InitAsync()
        {
            await UnityServices.InitializeAsync();
            AuthState authState = await AuthenticationWrapper.DoAuth();
            Debug.Log($"<color=black>UnityServices.InitializeAsync();AuthenticationWrapper.DoAuth() -> AuthState:{authState}</color>");
            if (authState == AuthState.Authenticated)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void GoToMenu()
        {
            SceneManager.LoadScene(NetworkingScenes.SCENE_MAIN_MENU);
        }
    }
}