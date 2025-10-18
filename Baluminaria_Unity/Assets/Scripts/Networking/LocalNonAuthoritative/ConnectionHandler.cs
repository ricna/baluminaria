using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Unrez
{
    public class ConnectionHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Button _buttonStartHost;
        [SerializeField]
        private Button _buttonStartClient;

        private void Start()
        {
            _buttonStartHost.onClick.AddListener(OnButtonStartHost);
            _buttonStartClient.onClick.AddListener(OnButtonStartClient);
        }

        private void OnDestroy()
        {
            _buttonStartHost.onClick.RemoveAllListeners();
            _buttonStartClient.onClick.RemoveAllListeners();
        }

        public void OnButtonStartHost()
        {
            Unbug.Log("OnButtonStartHost", Uncolor.Magenta);
            NetworkManager.Singleton.StartHost();
        }

        public void OnButtonStartClient()
        {
            Unbug.Log("OnButtonStartClient", Uncolor.Magenta);
            NetworkManager.Singleton.StartClient();
        }
    }
}