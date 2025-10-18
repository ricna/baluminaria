using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Unrez.Networking.UI
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
        /*
         * Using an IP address of 0.0.0.0 for the server listen address will make a server or host listen on all IP addresses assigned to the local system. 
         * This can be particularly helpful if you are testing a client instance on the same system as well as one or more client instances 
         * connecting from other systems on your local area network. 
         * Another scenario is while developing and debugging you might sometimes test local client instances on the same system 
         * and sometimes test client instances running on external systems.
         */
        public void OnButtonStartHost()
        {
            Unbug.Log("OnButtonStartHost", Uncolor.Magenta);
            //NetworkHandler.Instance.StartLocalHost();
            NetworkManager.Singleton.StartHost();

        }

        public void OnButtonStartClient()
        {
            Unbug.Log("OnButtonStartClient", Uncolor.Magenta);
            NetworkManager.Singleton.StartClient();
        }
    }
}