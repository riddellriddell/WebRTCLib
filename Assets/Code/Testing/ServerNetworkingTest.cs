using System.Collections;
using UnityEngine;

namespace Networking
{
    public class ServerNetworkingTest : MonoBehaviour
    {
        public MatchMakingServerSignaling m_mmsSignalling;

        public class CommandLineArgs
        {
            public string MatchMakingServerAddress;
        }

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(TestNetworkConnection());
        }

        public IEnumerator TestNetworkConnection()
        {
            yield return new WaitForEndOfFrame();

            // contact server and attempt to make connection 
            m_mmsSignalling.ConnectToMatchMakingServer();

            while( m_mmsSignalling.PeerRole != MatchMakingServerSignaling.Role.Listener || m_mmsSignalling.PeerRole != MatchMakingServerSignaling.Role.Connector)
            {
                yield return null;
            }

            while(m_mmsSignalling.PeerRole == MatchMakingServerSignaling.Role.Listener)
            {
                //try and get message from other end of connection
                m_mmsSignalling.

                yield return null;
            }

        }
    }
}