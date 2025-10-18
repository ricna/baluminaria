using System;
using Unity.Netcode;
using UnityEngine;

namespace Unrez.Networking
{
    /// <summary>
    /// Spawn a differente Prefab as desired.
    /// Remove the Player Prefab from the NetworkManager to use it.
    /// After to Connect the OnNetworkSpawn will be called here, then we will going to Spawn the "Player Prefab".
    /// </summary>
    public class PlayerSpawner : NetworkBehaviour
    {
        [Header("References")]
        [field: SerializeField]
        public bool TestCatOnly { get; private set; }
        [SerializeField]
        private GameObject _prefabDog;
        [SerializeField]
        private GameObject _prefabCat;
        [SerializeField]
        private SpawnPoints[] _spawnPoints;
        [SerializeField]
        private Transform _transformParent;

        public event Action<ulong, Baluminaria> OnPlayerSpawn;
        public event Action<ulong> OnPlayerDespawn;

        public override void OnNetworkSpawn()
        {
            Debug.Log("NetworkSpawn");
            if (TestCatOnly)
            {
                SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId, false);
                return;
            }
            if (IsServer)
            {
                SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId, true);
            }
            else
            {
                SpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId, false);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            OnPlayerDespawn?.Invoke(OwnerClientId);
        }

        [ServerRpc(RequireOwnership = false)] //server owns this object but client can request a spawn
        public void SpawnPlayerServerRpc(ulong clientId, bool dog)
        {
            GameObject newPlayer = default;
            int idxSpawns = UnityEngine.Random.Range(0, _spawnPoints.Length);
            _spawnPoints = FindObjectsByType(typeof(SpawnPoints), FindObjectsSortMode.InstanceID) as SpawnPoints[];
            Transform[] spawns = _spawnPoints[idxSpawns].Spawns;

            Debug.Log($"<color=green>SpawnPlayerServerRpc IsServer:{dog} clientId:{clientId}</color>");
            OnPlayerSpawn?.Invoke(OwnerClientId, newPlayer.GetComponent<Baluminaria>());
            newPlayer.transform.position = spawns[clientId].position;
            NetworkObject networkObject = newPlayer.GetComponent<NetworkObject>();
            newPlayer.SetActive(true);
            networkObject.SpawnAsPlayerObject(clientId, true);
        }
    }
}