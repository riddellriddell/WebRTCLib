using Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WebRtcConnectionTest : MonoBehaviour
{
    public class TestWebRtcConnection
    {
        public string m_strOffer = null;
        public string m_strReply = null;


        public WebRTCWrapper.State State { get; private set; } = WebRTCWrapper.State.WaitingToMakeOffer;

        public void SetOffer(string strOffer)
        {
            m_strOffer = strOffer;

            CheckIfWaitingToConenct();
        }

        public void SetReply(string strReply)
        {
            m_strReply = strReply;

            CheckIfWaitingToConenct();
        }
        
        public IEnumerator BuildOffer()
        {
            State = WebRTCWrapper.State.MakingOffer;

            yield return new WaitForSeconds(Random.Range(1.0f, 3.0f));

            m_strOffer = "test offer";

            State = WebRTCWrapper.State.WaitingForReply;
        }

        public IEnumerator BuildReply()
        {
            State = WebRTCWrapper.State.MakingReply;

            yield return new WaitForSeconds(Random.Range(1.0f, 3.0f));

            m_strReply = "test Reply";

            State = WebRTCWrapper.State.WaitingToConnect;
        }

        protected void CheckIfWaitingToConenct()
        {
            if(string.IsNullOrEmpty(m_strOffer) == false && string.IsNullOrEmpty(m_strReply) == false)
            {
                State = WebRTCWrapper.State.WaitingToConnect;
            }
        }
    }

    public bool m_bRunTest = false;

    public MatchMakingServerSignaling m_mssServerSignalling;

    public Dictionary<int,TestWebRtcConnection> m_twcConnections;
    public List<int> m_iConnectionsInProgress;

    // Start is called before the first frame update
    void Start()
    {
        GetLobbyAndProfile();
    }

    // Update is called once per frame
    void Update()
    {
        if(m_bRunTest)
        {
            Test();
            m_bRunTest = false;
        }

        //if(m_mssServerSignalling.PeerRole == MatchMakingServerSignaling.Role.Listener)
        //{
        //    ConnectAsLobby();
        //}
        //
        //if (m_mssServerSignalling.PeerRole == MatchMakingServerSignaling.Role.Connector)
        //{
        //    ConnectAsNewPlayer();
        //}
    }

    public void GetLobbyAndProfile()
    {
        // contact server 
        m_mssServerSignalling = new MatchMakingServerSignaling();
        m_mssServerSignalling.SetupDefaultNetworkingSettings();
        m_iConnectionsInProgress = new List<int>();

        m_twcConnections = new Dictionary<int, TestWebRtcConnection>();

        StartCoroutine(m_mssServerSignalling.ConnectToMatchMakingServer());
    }

    public void ConnectAsLobby()
    {
        // get messages
        StartCoroutine(m_mssServerSignalling.UpdateMessages());

        // check if message is connection offer
        while(m_mssServerSignalling.m_messagesRecieved.Count > 0)
        {
            // get message
            Tuple<int,string> tupMessage = m_mssServerSignalling.m_messagesRecieved.Dequeue();

            //create connection request
            TestWebRtcConnection twcConnection = new TestWebRtcConnection();

            //set offer
            twcConnection.SetOffer(tupMessage.Item2);

            Debug.Log("Recieved offer " + tupMessage.Item2);

            //start building reply
            StartCoroutine(twcConnection.BuildReply());

            // mark as still needing to send reply back
            m_iConnectionsInProgress.Add(tupMessage.Item1);

            //store connection
            m_twcConnections[tupMessage.Item1] = twcConnection;
        }

        //check if any connections have a reply ready 
        for(int i = m_twcConnections.Count -1; i > -1 ; i--)
        {
            int iTargetPlayerID = m_iConnectionsInProgress[i];

            TestWebRtcConnection twcConnection = m_twcConnections[iTargetPlayerID];

            //check if connection has finished making reply and is awaiting connection
            if(twcConnection.State == WebRTCWrapper.State.WaitingToConnect)
            {
                string strReply = twcConnection.m_strReply;
                               
                //send reply to other end of connection
                StartCoroutine(m_mssServerSignalling.SendMessage(
                    m_mssServerSignalling.m_plpPlayerProfile.Id,
                    iTargetPlayerID,
                    strReply));

                Debug.Log("Sending reply " + strReply);

                //remove connection from list of connections awaiting action
                m_iConnectionsInProgress.RemoveAt(i);
            }
        }
    }

    public void ConnectAsNewPlayer()
    {
        //check if conenction attempt is in progress
        if (m_twcConnections.Count == 0)
        {
            //create new connection
            TestWebRtcConnection twcConnection = new TestWebRtcConnection();

            //start making offer 
            StartCoroutine(twcConnection.BuildOffer());

            //add to list of connections in progress
            m_iConnectionsInProgress.Add(m_mssServerSignalling.m_plpPlayerProfile.Id);

            //add to list of connections
            m_twcConnections[m_mssServerSignalling.m_plpPlayerProfile.Id] = twcConnection;
        }

        for(int i = m_iConnectionsInProgress.Count -1; i > -1; i-- )
        {
            // id of connection
            int iConnectionId = m_iConnectionsInProgress[i];

            TestWebRtcConnection twcConnection = m_twcConnections[iConnectionId];

            //check if connection request has finished 
            if(twcConnection.State == WebRTCWrapper.State.WaitingForReply)
            {
                // get connection request
                string strConnectionOffer = twcConnection.m_strOffer;

                //send connection offer
                StartCoroutine(
                    m_mssServerSignalling.SendMessage(
                        iConnectionId, 
                        m_mssServerSignalling.m_glbGameLobby.OwnerId, 
                        strConnectionOffer)
                    );

                Debug.Log("Sending offer " + strConnectionOffer);

                //remove connection from list awaiting locat action
                m_iConnectionsInProgress.Remove(i);
            }
        }

        foreach(int ikey in m_twcConnections.Keys)
        {
            // get messages 
            StartCoroutine(m_mssServerSignalling.GetMessages(ikey));
        }

        // loop through all the messages
        while (m_mssServerSignalling.m_messagesRecieved.Count > 0)
        {
            Tuple<int, string> tupMessage = m_mssServerSignalling.m_messagesRecieved.Dequeue();

            //check if message from server 
            if(tupMessage.Item1 == m_mssServerSignalling.m_glbGameLobby.OwnerId)
            {
                //set it as reply 
                m_twcConnections[m_mssServerSignalling.m_plpPlayerProfile.Id].SetReply(tupMessage.Item2);

                Debug.Log("Recieved Rpley " + m_twcConnections[m_mssServerSignalling.m_plpPlayerProfile.Id].m_strReply);
            }
        }
    }

    public class testClass
    {
        public int item1;
        public string item2;
    }


    public void Test()
    {
        testClass testClass = new testClass() { item1 = 0, item2 = "Test" };

        string strJson = JsonUtility.ToJson(testClass);

        Debug.Log("Json String: " + strJson);
    }
}
