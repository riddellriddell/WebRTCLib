using Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityWebrtc.Marshalling;
using static UnityWebrtc.Webrtc;

public class WebRtcConnectionTest : MonoBehaviour
{
    public class TestWebRtcConnection
    {
        public List<ConfigurableIceServer> IceServers = new List<ConfigurableIceServer>()
        {
            new ConfigurableIceServer()
            {
                Type = IceType.Stun,
                Uri = "stun.l.google.com:19302"
            }
        };

        public WebRTCWrapper.State State
        {
            get
            {
                return m_rtwRTCWrapper.WebRtcObjectState;
            }
        }

        public WebRTCWrapper m_rtwRTCWrapper;

        public TestWebRtcConnection()
        {
            //UnityWebrtc.IPeer peePeer = new NativePeer(IceServers.Select(i => i.ToString()).ToList(), string.Empty, string.Empty);

            UnityWebrtc.IPeer peePeer = new SocketsPeer();


            peePeer.AddDataChannel();

            m_rtwRTCWrapper = new WebRTCWrapper(peePeer);
        }

        public IEnumerator BuildOffer()
        {
            m_rtwRTCWrapper.MakeOffer();

            while (m_rtwRTCWrapper.WebRtcObjectState == WebRTCWrapper.State.MakingOffer)
            {
                yield return null;
            }
        }

        public string GetOffer()
        {
            WebRTCWrapper.FullOffer fofOffer = m_rtwRTCWrapper.GetFullOffer();

            return JsonUtility.ToJson(fofOffer);
        }

        public IEnumerator ProcessOffer(string strOffer)
        {
            //deserialize offer
            WebRTCWrapper.FullOffer fofOffer = null;

            try
            {
                fofOffer = JsonUtility.FromJson<WebRTCWrapper.FullOffer>(strOffer);
            }
            catch
            {
                Debug.LogError($"Error Deserializing offer string {strOffer}");
                yield break;
            }

            if (fofOffer == null)
            {
                Debug.LogError("Offer deserialized to null");
                yield break;
            }

            m_rtwRTCWrapper.ProcessFullOffer(fofOffer);

            while (m_rtwRTCWrapper.WebRtcObjectState == WebRTCWrapper.State.MakingReply)
            {
                yield return null;
            }
        }

        public string GetReply()
        {
            WebRTCWrapper.FullReply frpReply = m_rtwRTCWrapper.GetFullReply();

            return JsonUtility.ToJson(frpReply);
        }

        public void SetReply(string strReply)
        {
            //deserialize reply
            WebRTCWrapper.FullReply frpReply = null;

            try
            {
                frpReply = JsonUtility.FromJson<WebRTCWrapper.FullReply>(strReply);
            }
            catch
            {
                Debug.LogError($"Error Deserializing offer string {strReply}");
            }

            if (frpReply == null)
            {
                Debug.LogError("reply deserialized to null");
                return;
            }

            m_rtwRTCWrapper.ProcessFullReply(frpReply);
        }

    }

    public Text m_txtOuput;

    public bool m_bRunTest = false;

    public MatchMakingServerSignaling m_mssServerSignalling;

    public Dictionary<int, TestWebRtcConnection> m_twcConnections;
    public List<int> m_iConnectionsInProgress;

    // Start is called before the first frame update
    void Start()
    {
        GetLobbyAndProfile();
    }

    // Update is called once per frame
    void Update()
    {
        if (m_bRunTest)
        {
            Test();
            m_bRunTest = false;
        }

        if (m_mssServerSignalling.PeerRole == MatchMakingServerSignaling.Role.Listener)
        {
            ConnectAsLobby();
        }

        if (m_mssServerSignalling.PeerRole == MatchMakingServerSignaling.Role.Connector)
        {
            ConnectAsNewPlayer();
        }

        string strOutput = "";
        string[] strThingOptions = new string[] { "\\", "|", "/", "-" };
        string strThink = strThingOptions[(int)(Time.timeSinceLevelLoad) % strThingOptions.Length];

        //check if any connection has succeded 
        foreach (KeyValuePair<int,TestWebRtcConnection> twcConnection in m_twcConnections)
        {
            strOutput += $" connection: {twcConnection.Key.ToString()} state: {twcConnection.Value.State.ToString()} {strThink}";

            if (twcConnection.Value.State == WebRTCWrapper.State.Conncted)
            {
                Debug.Log("Victory !!!!!!!!!!!!!!!!!!!!!!!!!");
            }
        }

        if(string.IsNullOrWhiteSpace(strOutput))
        {
            strOutput = $"SettingUP {strThink}";
        }

        m_txtOuput.text = strOutput;
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

        //update lobby 
        StartCoroutine(m_mssServerSignalling.UpdateGameLobby(m_mssServerSignalling.m_glbGameLobby.Id,0,0));

        // check if message is connection offer
        while (m_mssServerSignalling.m_messagesRecieved.Count > 0)
        {
            // get message
            Tuple<int, string> tupMessage = m_mssServerSignalling.m_messagesRecieved.Dequeue();

            //create connection request
            TestWebRtcConnection twcConnection = new TestWebRtcConnection();

            Debug.Log("Recieved offer " + tupMessage.Item2);

            //process offer and start building reply
            StartCoroutine(twcConnection.ProcessOffer(tupMessage.Item2));

            // mark as still needing to send reply back
            m_iConnectionsInProgress.Add(tupMessage.Item1);

            //store connection
            m_twcConnections[tupMessage.Item1] = twcConnection;
        }

        //check if any connections have a reply ready 
        for (int i = m_iConnectionsInProgress.Count - 1; i > -1; i--)
        {
            int iTargetPlayerID = m_iConnectionsInProgress[i];

            TestWebRtcConnection twcConnection = m_twcConnections[iTargetPlayerID];

            //check if connection has finished making reply and is awaiting connection
            if (twcConnection.State == WebRTCWrapper.State.WaitingToConnect)
            {
                string strReply = twcConnection.GetReply();

                //send reply to other end of connection
                StartCoroutine(m_mssServerSignalling.SendMessage(
                    m_mssServerSignalling.m_plpPlayerProfile.Id,
                    iTargetPlayerID,
                    strReply));

                Debug.Log($"Sending reply to {iTargetPlayerID} from {m_mssServerSignalling.m_plpPlayerProfile.Id} with value {strReply}");

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
            m_twcConnections.Add(m_mssServerSignalling.m_plpPlayerProfile.Id, twcConnection);
        }

        for (int i = m_iConnectionsInProgress.Count - 1; i > -1; i--)
        {
            // id of connection
            int iConnectionId = m_iConnectionsInProgress[i];

            TestWebRtcConnection twcConnection = m_twcConnections[iConnectionId];

            //check if connection request has finished 
            if (twcConnection.State == WebRTCWrapper.State.WaitingForReply)
            {
                // get connection request
                string strConnectionOffer = twcConnection.GetOffer();

                Debug.Log("Sending offer " + strConnectionOffer);

                //send connection offer
                StartCoroutine(
                    m_mssServerSignalling.SendMessage(
                        iConnectionId,
                        m_mssServerSignalling.m_glbGameLobby.OwnerId,
                        strConnectionOffer)
                    ); 

                //remove connection from list awaiting locat action
                m_iConnectionsInProgress.RemoveAt(i);
            }
        }

        // get messages 
        StartCoroutine(m_mssServerSignalling.UpdateMessages());


        // loop through all the messages
        while (m_mssServerSignalling.m_messagesRecieved.Count > 0)
        {
            Tuple<int, string> tupMessage = m_mssServerSignalling.m_messagesRecieved.Dequeue();

            //check if message from server 
            if (tupMessage.Item1 == m_mssServerSignalling.m_glbGameLobby.OwnerId)
            {
                //set it as reply 
                m_twcConnections[m_mssServerSignalling.m_plpPlayerProfile.Id].SetReply(tupMessage.Item2);

                Debug.Log("Recieved Rpley " + tupMessage.Item2);
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
        Debug.Log("Test Serialization");
        MessageGet messageGet = new MessageGet();
        messageGet.messages = new List<Message>();
        messageGet.messages.Add(new Message() { FromPlayerProfileId = 0, ToPlayerProfileId = 2, Id = 3, Time = 90000, Value = " Test" });

        string strSerializedData = JsonUtility.ToJson(messageGet);

        Debug.Log("results " + strSerializedData);
    }
}
