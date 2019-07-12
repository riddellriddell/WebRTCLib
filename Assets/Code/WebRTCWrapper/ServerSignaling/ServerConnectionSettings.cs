using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Networking
{
    [CreateAssetMenu(fileName = "Settings", menuName = "GameNetworking/MatchMakingSettings", order = 1)]
    public class ServerConnectionSettings : ScriptableObject
    {
        public string m_strMatchMakingServerAddress;
    }
}
