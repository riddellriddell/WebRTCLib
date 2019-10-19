using UnityEngine;

namespace Networking
{
    [System.Serializable]
    public class CreateLobbyClass
    {
        [SerializeField]
        public PlayerProfile playerProfile;
        [SerializeField]
        public GameLobby gameLobby;
    }
}
