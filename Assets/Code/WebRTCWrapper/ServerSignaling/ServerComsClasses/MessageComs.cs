using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    [System.Serializable]
    public class MessageSend
    {
        public int fromId;
        public int toId;
        public string message;
    }

    [System.Serializable]
    public class MessageGet
    {
        [SerializeField]
        public List<Message> messages;
    }
}
