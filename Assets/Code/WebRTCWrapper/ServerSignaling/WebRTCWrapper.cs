#define MAKE_LOGS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityWebrtc;



namespace Networking
{ 
    //this class converts the the event based interface of webrtc to a "simper" pole and cache system
    public class WebRTCWrapper
    {
        public enum State
        {
            WaitingToMakeOffer,
            MakingOffer,
            MakingReply,
            WaitingForReply,
            WaitingToConnect,
            Conncted,
            Disconnected,
            Failed
        }


        public List<string> m_strMessages;

        public Tuple<string,string> m_tupOffer;

        public Tuple<string, string> m_tupReply;

        public List<Tuple<string,int,string>> m_tupIceCandidates = new List<Tuple<string, int, string>>();

        public DateTime m_dtmTimeOfLastIceCandidate;

        public float m_fIceWaitTime = 5f;

        public IPeer m_perUnderlyingNetworkPeer;
        
        public State WebRtcObjectState
        {
            get
            {
                //check if making offer has timed out or finished
                TimeSpan tspTimeSinceLastIceCandidate = DateTime.UtcNow - m_dtmTimeOfLastIceCandidate;
                float fTimeSinceLastIce = (float)tspTimeSinceLastIceCandidate.TotalSeconds;

                switch (m_webRtcObjectState)
                {
                    case State.MakingOffer:
                        if(fTimeSinceLastIce > m_fIceWaitTime && m_tupIceCandidates.Count > 0)
                        {
                            //offer has finished
                            m_webRtcObjectState = State.WaitingForReply;
                        }
                        break;
                    case State.MakingReply:
                        if (fTimeSinceLastIce > m_fIceWaitTime && m_tupIceCandidates.Count > 0)
                        {
                            //offer has finished
                            m_webRtcObjectState = State.WaitingToConnect;
                        }
                        break;
                }

                return m_webRtcObjectState;
            }
        }
        protected State m_webRtcObjectState = State.WaitingToMakeOffer;

        public WebRTCWrapper(IPeer perNativeWebRtcLayer)
        {
            m_perUnderlyingNetworkPeer = perNativeWebRtcLayer;

            //add events for data channel
            m_perUnderlyingNetworkPeer.LocalDataChannelReady += DataChannelConnected;
            m_perUnderlyingNetworkPeer.DataFromDataChannelReady += OnDataReceived;

            //add events for error
            m_perUnderlyingNetworkPeer.FailureMessage += Error;
        }

        public void MakeOffer()
        {
            //add data channel ( Replace with custom build of the API later)
            m_perUnderlyingNetworkPeer.AddDataChannel();

            m_dtmTimeOfLastIceCandidate = DateTime.UtcNow;
            m_perUnderlyingNetworkPeer.LocalSdpReadytoSend -= OnOfferFinished;
            m_perUnderlyingNetworkPeer.LocalSdpReadytoSend += OnOfferFinished;
            m_perUnderlyingNetworkPeer.IceCandiateReadytoSend -= OnIceFinished;
            m_perUnderlyingNetworkPeer.IceCandiateReadytoSend += OnIceFinished;
            m_perUnderlyingNetworkPeer.CreateOffer();
            m_webRtcObjectState = State.MakingOffer;
        }

        public void ProcessOfferAndMakeReply(string strOfferValue)
        {
#if MAKE_LOGS
            Debug.Log($"Processing Offer {strOfferValue} and building reply");
#endif
            m_dtmTimeOfLastIceCandidate = DateTime.UtcNow;
            m_perUnderlyingNetworkPeer.LocalSdpReadytoSend -= OnReplyFinished;
            m_perUnderlyingNetworkPeer.LocalSdpReadytoSend += OnReplyFinished;
            m_perUnderlyingNetworkPeer.IceCandiateReadytoSend -= OnIceFinished;
            m_perUnderlyingNetworkPeer.IceCandiateReadytoSend += OnIceFinished;
            m_perUnderlyingNetworkPeer.SetRemoteDescription("offer", strOfferValue);
            m_perUnderlyingNetworkPeer.CreateAnswer();
            m_webRtcObjectState = State.MakingReply;
        }

        public void ProcessReply(string strReplyValue)
        {
#if MAKE_LOGS
            Debug.Log($"Processing Answer {strReplyValue}");
#endif
            m_perUnderlyingNetworkPeer.SetRemoteDescription("answer", strReplyValue);
        }

        public void AddICE(string strType, int iIndex, string strIceValue)
        {
#if MAKE_LOGS
            Debug.Log($"Processing Ice type:{strType} index: {iIndex} value: {strIceValue}");
#endif
            m_perUnderlyingNetworkPeer.AddIceCandidate(strType, iIndex, strIceValue);
        }

        public void SendData(string strData)
        {
#if MAKE_LOGS
            Debug.Log($"Sending Data {strData}");
#endif

            m_perUnderlyingNetworkPeer.SendDataViaDataChannel(strData);
        }

        protected void OnOfferFinished(string strType, string strOffer)
        {
#if MAKE_LOGS
            Debug.Log($"New offer Type: {strType} Value: {strOffer}");
#endif
            m_tupOffer = new Tuple<string,string>(strType,strOffer);
        }

        protected void OnReplyFinished(string strType, string strReply)
        {
#if MAKE_LOGS
            Debug.Log($"New reply Type: {strType} Value: {strReply}");
#endif
            m_tupReply = new Tuple<string, string>(strType, strReply);
        }

        protected void OnIceFinished(string strType, int iIndex, string strValue)
        {
#if MAKE_LOGS
            Debug.Log($"New Ice candidate Type: {strType} Index: {iIndex} Value: {strValue}");
#endif

            m_tupIceCandidates.Add(new Tuple<string, int, string>(strType, iIndex, strValue));
            m_dtmTimeOfLastIceCandidate = DateTime.UtcNow;
        }

        protected void DataChannelConnected()
        {
#if MAKE_LOGS
            Debug.Log("Data channel opened");
#endif
            m_webRtcObjectState = State.Conncted;
        }

        protected void OnDataReceived(string strData)
        {
#if MAKE_LOGS
            Debug.Log($"Data recieved {strData}");
#endif
        }

        protected void Error(string strError)
        {
#if MAKE_LOGS
            Debug.LogError(strError);
#endif
        }

    }
}
