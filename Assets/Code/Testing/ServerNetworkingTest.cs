using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Networking
{
    public class ServerNetworkingTest : MonoBehaviour
    {
        protected MatchMakingServerSignaling m_mmsSignalling;

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
            m_mmsSignalling = new MatchMakingServerSignaling();
            m_mmsSignalling.SetupDefaultNetworkingSettings();

             // contact server and attempt to make connection 
             yield return m_mmsSignalling.ConnectToMatchMakingServer();

            while( m_mmsSignalling.PeerRole != MatchMakingServerSignaling.Role.Listener || m_mmsSignalling.PeerRole != MatchMakingServerSignaling.Role.Connector)
            {
                yield return null;
            }

            if(m_mmsSignalling.PeerRole == MatchMakingServerSignaling.Role.Connector)
            {
                int iFromID = m_mmsSignalling.m_plpPlayerProfile.Id;
                int iToID = m_mmsSignalling.m_glbGameLobby.OwnerId;

               m_mmsSignalling.SendMessage(iFromID, iToID, "TestMessage");
            }

            while(m_mmsSignalling.PeerRole == MatchMakingServerSignaling.Role.Listener)
            {
                //try and get message from other end of connection
                if(m_mmsSignalling.m_messagesRecieved.Count > 0)
                {
                    //pop message 
                    Tuple<int, string> tupMessage = m_mmsSignalling.m_messagesRecieved.Dequeue();

                    Debug.Log($"Message recieved from {tupMessage.Item1}, message: {tupMessage.Item2}");
                }

                yield return null;
            }

        }
    }
}