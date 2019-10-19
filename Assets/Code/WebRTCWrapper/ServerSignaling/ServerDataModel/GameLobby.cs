using System;

namespace Networking
{
    //this represents a game connection node
    [System.Serializable]
    public class GameLobby
    {
        public int Id;

        public int OwnerId;

        public int PlayersInLobby;

        public int State;

        public long TimeOfLastActivity;
    }
}
