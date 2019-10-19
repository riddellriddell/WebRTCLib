#define MAKE_LOGS

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityWebrtc;



namespace Networking
{
    //this class converts the the event based interface of webrtc to a "simper" pole and cache system
    public class WebRTCWrapper
    {
        [System.Serializable]
        public class Offer
        {
            public string m_strType;
            public string m_strValue;
        }

        [System.Serializable]
        public class Reply
        {
            public string m_strType;
            public string m_strValue;
        }

        [System.Serializable]
        public class IceCandidate
        {
            public string m_strType;
            public int m_iIndex;
            public string m_strValue;
        }

        [System.Serializable]
        public class FullOffer
        {
            public Offer m_ofrOffer;

            public List<IceCandidate> m_iceCandidates;
        }

        [System.Serializable]
        public class FullReply
        {
            public Reply m_rptReply;

            public List<IceCandidate> m_iceCandidates;
        }

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

        public Offer m_ofrOffer;

        public Reply m_repReply;

        public List<IceCandidate> m_iceOfferIceCandidates = new List<IceCandidate>();

        public List<IceCandidate> m_iceReplyIceCandidates = new List<IceCandidate>();

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
                        if (fTimeSinceLastIce > m_fIceWaitTime )
                        {
                            //offer has finished
                            m_webRtcObjectState = State.WaitingForReply;
                        }
                        break;
                    case State.MakingReply:
                        if (fTimeSinceLastIce > m_fIceWaitTime )
                        {
                            //reply has finished
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
            m_perUnderlyingNetworkPeer.IceCandiateReadytoSend -= OnOfferIceFinished;
            m_perUnderlyingNetworkPeer.IceCandiateReadytoSend += OnOfferIceFinished;
            m_perUnderlyingNetworkPeer.CreateOffer();
            m_webRtcObjectState = State.MakingOffer;
        }

        public FullOffer GetFullOffer()
        {
            FullOffer floOffer = new FullOffer()
            {
                m_ofrOffer = m_ofrOffer,
                m_iceCandidates = m_iceOfferIceCandidates
            };

            return floOffer;
        }

        public void ProcessFullOffer(FullOffer fofFullOffer)
        {
            //process base offer
            ProcessOfferAndMakeReply(fofFullOffer.m_ofrOffer.m_strValue);

            //process all the ice candidates
            for (int i = 0; i < fofFullOffer.m_iceCandidates.Count; i++)
            {
                IceCandidate iceCandidate = fofFullOffer.m_iceCandidates[i];

                AddICE(iceCandidate.m_strType, iceCandidate.m_iIndex, iceCandidate.m_strValue);
            }
        }

        public void ProcessOfferAndMakeReply(string strOfferValue)
        {
#if MAKE_LOGS
            Debug.Log($"Processing Offer {strOfferValue} and building reply");
#endif
            m_dtmTimeOfLastIceCandidate = DateTime.UtcNow;
            m_perUnderlyingNetworkPeer.LocalSdpReadytoSend -= OnReplyFinished;
            m_perUnderlyingNetworkPeer.LocalSdpReadytoSend += OnReplyFinished;
            m_perUnderlyingNetworkPeer.IceCandiateReadytoSend -= OnReplyIceFinished;
            m_perUnderlyingNetworkPeer.IceCandiateReadytoSend += OnReplyIceFinished;
            m_perUnderlyingNetworkPeer.SetRemoteDescription("offer", strOfferValue);
            m_perUnderlyingNetworkPeer.CreateAnswer();
            m_webRtcObjectState = State.MakingReply;
        }

        public FullReply GetFullReply()
        {
            FullReply frpOffer = new FullReply()
            {
                m_rptReply = m_repReply,
                m_iceCandidates = m_iceReplyIceCandidates
            };

            return frpOffer;
        }

        public void ProcessFullReply(FullReply frpFullReply)
        {
            //process base offer
            ProcessReply(frpFullReply.m_rptReply.m_strValue);

            //process all the ice candidates
            for (int i = 0; i < frpFullReply.m_iceCandidates.Count; i++)
            {
                IceCandidate iceCandidate = frpFullReply.m_iceCandidates[i];

                AddICE(iceCandidate.m_strType, iceCandidate.m_iIndex, iceCandidate.m_strValue);
            }

            m_webRtcObjectState = State.WaitingToConnect;
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
            m_ofrOffer = new Offer() { m_strType = strType, m_strValue = strOffer };
        }

        protected void OnReplyFinished(string strType, string strReply)
        {
#if MAKE_LOGS
            Debug.Log($"New reply Type: {strType} Value: {strReply}");
#endif
            m_repReply = new Reply() { m_strType = strType, m_strValue = strReply };
        }

        protected void OnOfferIceFinished(string strType, int iIndex, string strValue)
        {
#if MAKE_LOGS
            Debug.Log($"New offer Ice candidate Type: {strType} Index: {iIndex} Value: {strValue}");
#endif

            m_iceOfferIceCandidates.Add(new IceCandidate() { m_strType = strType, m_iIndex = iIndex, m_strValue = strValue });
            m_dtmTimeOfLastIceCandidate = DateTime.UtcNow;
        }

        protected void OnReplyIceFinished(string strType, int iIndex, string strValue)
        {
#if MAKE_LOGS
            Debug.Log($"New reply Ice candidate Type: {strType} Index: {iIndex} Value: {strValue}");
#endif

            m_iceReplyIceCandidates.Add(new IceCandidate() { m_strType = strType, m_iIndex = iIndex, m_strValue = strValue });
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
