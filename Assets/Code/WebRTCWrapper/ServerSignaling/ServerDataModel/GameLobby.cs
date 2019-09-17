using System;

namespace Networking
{
    //this represents a game connection node
    public class GameLobby
    {
        public int Id { get; set; }

        public int OwnerId { get; set; }
        
        public int PlayersInLobby { get; set; }

        public int State { get; set; }

        public DateTime TimeOfLastActivity { get; set; }
    }
}
