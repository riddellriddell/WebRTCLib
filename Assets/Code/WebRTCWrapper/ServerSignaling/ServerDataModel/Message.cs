using System;

namespace Networking
{
    [System.Serializable]
    public class Message
    {
        public int Id;
        public long Time;
        public int ToPlayerProfileId;
        public int FromPlayerProfileId;
        public string Value;
    }
}
