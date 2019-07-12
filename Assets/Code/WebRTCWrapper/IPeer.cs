using System;

namespace UnityWebrtc
{
    public interface IPeerImpl
    {
        /// <summary>
        /// Close the peer connection
        /// </summary>
        void ClosePeerConnection();

        /// <summary>
        /// Get the unique id
        /// </summary>
        /// <returns>unique id</returns>
        int GetUniqueId();

        /// <summary>
        /// Add a data channel
        /// </summary>
        void AddDataChannel();

        /// <summary>
        /// Create an sdp offer
        /// </summary>
        void CreateOffer();

        /// <summary>
        /// Create an sdp answer
        /// </summary>
        void CreateAnswer();

        /// <summary>
        /// Send data via the created data channel
        /// </summary>
        /// <remarks>
        /// Must call <see cref="AddDataChannel"/> first
        /// </remarks>
        /// <param name="data">data to send</param>
        void SendDataViaDataChannel(string data);

        /// <summary>
        /// Set the remote stream description
        /// </summary>
        /// <remarks>
        /// Known valid types are "answer" and "offer"
        /// </remarks>
        /// <param name="type">description type</param>
        /// <param name="sdp">sdp data</param>
        void SetRemoteDescription(string type, string sdp);

        /// <summary>
        /// Add an ice candidate
        /// </summary>
        /// <param name="candidate">sdp candidate data</param>
        /// <param name="sdpMlineindex">sdp mline index</param>
        /// <param name="sdpMid">sdp mid</param>
        void AddIceCandidate(string candidate, int sdpMlineindex, string sdpMid);
    }

    public interface IPeer : IPeerImpl
    {
        /// <summary>
        /// Event that occurs when the local data channel is ready
        /// </summary>
        event Action LocalDataChannelReady;

        /// <summary>
        /// Event that occurs when data is available from the remote, sent via the data channel
        /// </summary>
        event Action<string> DataFromDataChannelReady;

        /// <summary>
        /// Event that occurs when a native failure occurs
        /// </summary>
        event Action<string> FailureMessage;

        /// <summary>
        /// Event that occurs when a local SDP is ready to transmit
        /// </summary>
        event Action<string, string> LocalSdpReadytoSend;

        /// <summary>
        /// Event that occurs when a local ice candidate is ready to transmit
        /// </summary>
        event Action<string, int, string> IceCandiateReadytoSend;
    }
}
