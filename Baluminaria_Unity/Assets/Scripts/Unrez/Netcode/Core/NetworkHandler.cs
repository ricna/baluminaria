using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unrez.Essential;

namespace Unrez.Networking
{

    /// <summary>
    /// NetworkRigidbody is a component that sets the Rigidbody of the GameObject into kinematic mode on all non-authoritative instances
    /// (except the instance that has authority). 
    /// Authority is determined by the NetworkTransform component(required) attached to the same GameObject as the NetworkRigidbody.
    /// Whether the NetworkTransform is server authoritative (default) or owner authoritative, the NetworkRigidBody authority model will mirror it.
    /// That way, the physics simulation runs on the authoritative instance, and the resulting positions synchronize on the non-authoritative instances, 
    /// each with their RigidBody being Kinematic, without any interference.
    /// </summary>

    [RequireComponent(typeof(NetworkManager))]
    public class NetworkHandler : Singleton<NetworkHandler>
    {
        [SerializeField]
        private string _ipLocal;
        [SerializeField]
        private ushort _port = 7777;

        private void Start()
        {
            _ipLocal = GetLocalIPAddress();
        }

        public string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        public void InitialiseTransport(string ip, ushort port)
        {
            UnityTransport unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            unityTransport.SetConnectionData(ip, port);
        }

        public void StartLocalHost(ushort port = 7777)
        {
            UnityTransport unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            unityTransport.SetConnectionData(_ipLocal, port);
            NetworkManager.Singleton.StartHost();
        }
    }
}
