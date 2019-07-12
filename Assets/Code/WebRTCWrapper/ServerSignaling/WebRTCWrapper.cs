using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Code.WebRTCWrapper.ServerSignaling
{
    //this class converts the the event based interface of webrtc to a "simper" pole and cache system
    public class WebRTCWrapper
    {
        public enum State
        {
            WaitingToMakeOffer,
            MakingOffer,
            MakingIceForOffer,
            MakingReply,
            MakingIceForReply,
            WaitingForOffer,
            WaitingForReply,
            Conncted,
            Disconnected,
            Failed
        }

        public State m_webRtcObjectState;

        public List<string> m_strMessages;

        public string m_strOffer;

        public string m_strReply;

        public List<string> m_strIceCandidates;

        public IPeer m_underlyingNetworkPeer;

        public void MakeOffer()
        {

        }

        public void ProcessOfferAndMakeReply(string strOffer)
        {

        }

        public void ProcessReply(string strReply)
        {

        }

        public void AddICE(string strIceCandidate)
        {

        }

        protected void OnOfferFinished(string strOffer)
        {
            m_strOffer = strOffer;
        }

        protected void OnReplyFinished(string strReply)
        {
            m_strReply = strReply;
        }
    }
}
