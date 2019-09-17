using System;

namespace Networking
{
    public class Message
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public int ToPlayerProfileId { get; set; }
        public int FromPlayerProfileId { get; set; }
        public string Value { get; set; }
    }
}
