using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityWebrtc;

public class SocketsPeer : IPeer
{
    [System.Serializable]
    public class Address
    {
        public string address;
        public int port;
    }

    public struct UdpState
    {
        public UdpClient udpClient;
        public IPEndPoint eptEndPoint;
    }

    public const int c_iMaxDataLength = 500;
    private const string c_strConnectionType = "UdpSocketCon";
    private const string c_strConnectionOpenMessage = "OpenDataChannel";
    private const string c_strOfferMessageType = "offer";
    private const string c_strAnswerMessageType = "answer";

    private static TimeSpan s_tspTimeBetweenConnectionResends = TimeSpan.FromSeconds(0.5f);

    public event Action LocalDataChannelReady;
    public event Action<string> DataFromDataChannelReady;
    public event Action<string> FailureMessage;
    public event Action<string, string> LocalSdpReadytoSend;
    public event Action<string, int, string> IceCandiateReadytoSend;

    private UdpClient m_udpUdpListen;
    private UdpClient m_udpUdpSend;
    private bool m_bListeningForMessages;
    private bool m_bDataChannelConnected = false;
    private DateTime m_dtmTimeOfLastConnectionCheck = DateTime.MinValue;
    private Queue<string> m_strSendDataBuffer = new Queue<string>();
    private bool m_bSendingData = false;

    public void Update()
    {

    }

    public void AddDataChannel()
    {
        Debug.Log("Data channel is included by default with Socket Peer no need to add");
    }

    public void AddIceCandidate(string candidate, int sdpMlineindex, string sdpMid)
    {
        Debug.Log("No ice candidates are needed");
    }

    public void ClosePeerConnection()
    {
        if (m_udpUdpListen != null)
        {
            m_udpUdpListen.Close();
            m_udpUdpListen = null;
        }

        if (m_udpUdpSend != null)
        {
            m_udpUdpSend.Close();
            m_udpUdpSend = null;
        }
    }

    public void CreateAnswer()
    {
        //get listening port values
        int iOpenPort = GetOpenPort();

        IPAddress ipAddress = GetLocalIPAddress();
        
        Address ipaAddress = new Address() { address = ipAddress.ToString(), port = iOpenPort };

        if (ipAddress == null)
        {
            Debug.LogError("Failed to get ip address");

            return;
        }

        try
        {
            //create listen port
            BeginReceivingMessages(iOpenPort);
        }
        catch
        {
            Debug.Log("Failed to create offer");
        }

        try
        {
            string strOffer = JsonUtility.ToJson(ipaAddress);

            //fire event for offer made
            LocalSdpReadytoSend?.Invoke(c_strConnectionType, strOffer);
        }
        catch
        {
            Debug.LogError("Error serializing answer message");
        }
    }

    public void CreateOffer()
    {
        int iOpenPort = GetOpenPort();

        IPAddress ipAddress = GetLocalIPAddress();

        Address ipaAddress = new Address() { address = ipAddress.ToString(), port = iOpenPort };

        if (ipAddress == null)
        {
            Debug.LogError("Failed to get ip address");

            return;
        }

        try
        {
            //create listen port
            BeginReceivingMessages(iOpenPort);
        }
        catch(Exception excException)
        {
            Debug.LogError($"Failed to setup listener due to error{excException.Message}");

            return;
        }

        try
        {
            string strOffer = JsonUtility.ToJson(ipaAddress);

            //fire event for offer made
            LocalSdpReadytoSend?.Invoke(c_strConnectionType, strOffer);
        }
        catch
        {
            Debug.LogError("Error serializing offer message");
        }

    }

    public int GetUniqueId()
    {
        throw new NotImplementedException();
    }

    public void SendDataViaDataChannel(string strData)
    {
        if( m_udpUdpSend == null)
        {
            Debug.LogError($"Connection not setup unable to send message {strData}");
            return;
        }

        //check if not currently sending data 
        Debug.Log($"Queuing message {strData} to send");
        m_strSendDataBuffer.Enqueue(strData);
        m_bSendingData = true;
        StartMessageSend();
    }

    public void SetRemoteDescription(string type, string sdp)
    {
        Address ipaAddress = null;

        try
        {
            ipaAddress = JsonUtility.FromJson<Address>(sdp);
        }
        catch
        {
            Debug.LogError("Error deserializing offer");
            return;
        }

        if(ipaAddress == null)
        {
            Debug.LogError("Failed to deserialize IpAddres Message");
            return;
        }  

        int iSendPort = GetOpenPort();

        try
        {           
            Debug.Log($"Creating connection for send on port {iSendPort.ToString()} ");

            //create udp port to send data on
            m_udpUdpSend = new UdpClient(iSendPort);
        }
        catch (Exception excException)
        {
            Debug.LogError($"Failed to create send connection on port {iSendPort.ToString()} due to {excException.Message}");
            throw;
        }


        IPAddress ipTargetAddress = IPAddress.Parse(ipaAddress.address);

        Debug.Log($"Connectiong to remote peer at address {ipaAddress.address} on port {ipaAddress.port.ToString()}");

        try
        {
            m_udpUdpSend.Connect(ipTargetAddress, ipaAddress.port);
        }
        catch(Exception excException)
        {
            Debug.LogError($"Failed to connect to address ip: {ipTargetAddress.ToString()} port:{ipaAddress.port.ToString()} due to error {excException.Message}");
        }

        //try and send inital connect message
        SendDataViaDataChannel(c_strConnectionOpenMessage);
    }
    
    private IPAddress GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip;
            }
        }

        Debug.LogError("Unable to find local IP");

        return null;
    }

    private int GetOpenPort()
    {
        int PortStartIndex = 1000;
        int PortEndIndex = 2000;
        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
        IPEndPoint[] tcpEndPoints = properties.GetActiveTcpListeners();
        IPEndPoint[] udpEndPoints = properties.GetActiveUdpListeners();

        List<int> usedPorts = tcpEndPoints.Select(p => p.Port).ToList<int>();
        usedPorts.AddRange(udpEndPoints.Select(p => p.Port).ToList<int>());

        int unusedPort = 0;

        for (int port = PortStartIndex; port < PortEndIndex; port++)
        {
            if (!usedPorts.Contains(port))
            {
                unusedPort = port;
                break;
            }
        }
        return unusedPort;
    }

    private void BeginReceivingMessages(int iListenPort)
    {
        // Receive a message and write it to the console.
        IPEndPoint eptEndPoint = new IPEndPoint(IPAddress.Any, iListenPort);

        try
        {
            Debug.Log($"Creating connection for address {eptEndPoint.ToString()}");
            m_udpUdpListen = new UdpClient(eptEndPoint);

        }
        catch(Exception excException)
        {
            Debug.LogError($"Failed to setup message recieve connection for address {eptEndPoint.ToString()} due to {excException.Message}");
            throw;
        }

        UdpState ustUdpState = new UdpState();
        ustUdpState.eptEndPoint = eptEndPoint;
        ustUdpState.udpClient = m_udpUdpListen;

        Console.WriteLine("listening for messages");
        m_udpUdpListen.BeginReceive(new AsyncCallback(OnMessageReceived), ustUdpState);

        m_bListeningForMessages = true;
    }

    private void OnMessageReceived(IAsyncResult asrAsyncResult)
    {
        UdpState udsState = (UdpState)(asrAsyncResult.AsyncState);
        UdpClient udpClient = udsState.udpClient;
        IPEndPoint eptEndPoint = udsState.eptEndPoint;
        
        byte[] receiveBytes = udpClient.EndReceive(asrAsyncResult, ref eptEndPoint);
        string receiveString = Encoding.ASCII.GetString(receiveBytes);

        //indicate coms channel is open
        if (m_bDataChannelConnected == false)
        {
            m_bDataChannelConnected = true;

            //send message back confirming data channel is open 
            SendDataViaDataChannel(c_strConnectionOpenMessage);

            //indicate that connection is open
            LocalDataChannelReady?.Invoke();
        }

        if (receiveString != c_strConnectionOpenMessage)
        {
            DataFromDataChannelReady?.Invoke(receiveString);
        }       

        //restart listening for messages 
        udpClient.BeginReceive(new AsyncCallback(OnMessageReceived), udsState);

        Console.WriteLine($"Received: {receiveString}");
    }

    private void StartMessageSend()
    {
        string strMessage = m_strSendDataBuffer.Dequeue();


        byte[] bData = Encoding.ASCII.GetBytes(strMessage);

        if (bData.Length > c_iMaxDataLength)
        {
            Debug.LogError($"Message {strMessage} Is {bData.Length.ToString()} bytes long, longer than max message size of {c_iMaxDataLength}");
            return;
        }

        m_udpUdpSend.BeginSend(bData, bData.Length,new AsyncCallback(OnMessageSendEnd),null);
    }

    private void OnMessageSendEnd(IAsyncResult asrAsyncResult)
    {
        Debug.Log("Message sent");

        //check if there are more messages to send 
        if(m_strSendDataBuffer.Count > 0)
        {
            //send next message
            StartMessageSend();
        }

        m_bSendingData = false; 
    }
}
