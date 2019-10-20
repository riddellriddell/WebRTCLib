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

    [System.Serializable]
    public class UpdateLobby
    {
        [SerializeField]
        public int id;
        [SerializeField]
        public int playerCount;
        [SerializeField]
        public int state;
    }
}
