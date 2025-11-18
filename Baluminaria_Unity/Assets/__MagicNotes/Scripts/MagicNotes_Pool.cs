using System.Collections.Generic;
using UnityEngine;

public class MagicNotes_Pool : MonoBehaviour
{
    [System.Serializable]
    public class PoolItem
    {
        public string poolId;
        public GameObject prefab;
        public int preloadCount = 10;
    }

    public static MagicNotes_Pool Instance;

    [SerializeField] private List<PoolItem> pools;
    private Dictionary<string, Queue<GameObject>> dictionary;

    private void Awake()
    {
        Instance = this;
        dictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (PoolItem p in pools)
        {
            Queue<GameObject> q = new Queue<GameObject>();
            for (int i = 0; i < p.preloadCount; i++)
            {
                GameObject obj = Instantiate(p.prefab);
                obj.SetActive(false);
                q.Enqueue(obj);
            }
            dictionary.Add(p.poolId, q);
        }
    }

    public GameObject Spawn(string poolId, Vector3 pos, Quaternion rot)
    {
        if (!dictionary.ContainsKey(poolId))
        {
            Debug.LogError("POOL NÃO ENCONTRADO: " + poolId);
            return null;
        }

        GameObject obj;

        if (dictionary[poolId].Count > 0)
        {
            obj = dictionary[poolId].Dequeue();
        }
        else
        {
            obj = Instantiate(GetPrefab(poolId));
        }

        obj.transform.position = pos;
        obj.transform.rotation = rot;
        obj.SetActive(true);

        return obj;
    }

    public void Despawn(string poolId, GameObject obj)
    {
        obj.SetActive(false);
        dictionary[poolId].Enqueue(obj);
    }

    private GameObject GetPrefab(string id)
    {
        foreach (PoolItem p in pools)
        {
            if (p.poolId == id)
                return p.prefab;
        }
        return null;
    }
}
