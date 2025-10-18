using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Unrez.Networking
{

    public class LobbyItem : MonoBehaviour
    {

        private Lobby _lobby;
        private Lobbies _lobbies;
        [SerializeField]
        private TMP_Text _textLobbyName;
        [SerializeField]
        private TMP_Text _textLobbyPlayers;
        [SerializeField]
        private Button _buttonJoinLobby;


        private void Start()
        {
            _buttonJoinLobby.onClick.AddListener(Join);
        }

        private void Join()
        {
            _lobbies.JoinAsync(_lobby);
        }

        public void Initialize(Lobbies lobbiesList, Lobby lobby)
        {
            _lobbies = lobbiesList;
            _lobby = lobby;
            _textLobbyName.text = _lobby.Name + $" [Time: {Time.time} ]";
            _textLobbyPlayers.text = $"( {_lobby.Players.Count}/{_lobby.MaxPlayers})";
        }
    }
}