using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadBootstrap : MonoBehaviour
{
    [SerializeField]
    private GameObject _goMatchManager;

    private void Awake()
    {
        _goMatchManager.SetActive(false);
    }

    private void Start()
    {
        NetworkManager networkManager = FindAnyObjectByType<NetworkManager>();
        if (networkManager == null)
        {
            SceneManager.LoadScene(0);
        }
        else
        {
            _goMatchManager.SetActive(true);
            Destroy(this.gameObject);
        }
    }
}
