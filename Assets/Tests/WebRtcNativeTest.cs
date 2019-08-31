using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityWebrtc;
using UnityWebrtc.Marshalling;

namespace Tests
{
    public class WebRtcNativeTest
    {

        /// <summary>
        /// Different Ice server types
        /// </summary>
        public enum IceType
        {
            /// <summary>
            /// Indicates there is no Ice information
            /// </summary>
            /// <remarks>
            /// Under normal use, this should not be used
            /// </remarks>
            None = 0,

            /// <summary>
            /// Indicates Ice information is of type STUN
            /// </summary>
            /// <remarks>
            /// https://en.wikipedia.org/wiki/STUN
            /// </remarks>
            Stun,

            /// <summary>
            /// Indicates Ice information is of type TURN
            /// </summary>
            /// <remarks>
            /// https://en.wikipedia.org/wiki/Traversal_Using_Relays_around_NAT
            /// </remarks>
            Turn
        }

        /// <summary>
        /// Represents an Ice server in a simple way that allows configuration from the unity inspector
        /// </summary>
        [Serializable]
        public struct ConfigurableIceServer
        {
            /// <summary>
            /// The type of the server
            /// </summary>
            public IceType Type;

            /// <summary>
            /// The unqualified uri of the server
            /// </summary>
            /// <remarks>
            /// You should not prefix this with "stun:" or "turn:"
            /// </remarks>
            public string Uri;

            /// <summary>
            /// Convert the server to the representation the underlying libraries use
            /// </summary>
            /// <returns>stringified server information</returns>
            public override string ToString()
            {
                return string.Format("{0}: {1}", Type.ToString().ToLower(), Uri);
            }
        }

        /// <summary>
        /// Set of ice servers
        /// </summary>
        [Tooltip("(Optional) Set of Ice servers")]
        public List<ConfigurableIceServer> IceServers = new List<ConfigurableIceServer>()
        {
            new ConfigurableIceServer()
            {
                Type = IceType.Stun,
                Uri = "stun.l.google.com:19302"
            }
        };

        // A Test behaves as an ordinary method
        [Test]
        public void WebRtcNativeTestSimplePasses()
        {
            // Use the Assert class to test conditions
            IPeer nativePeer = new NativePeer(IceServers.Select(i => i.ToString()).ToList(), string.Empty, string.Empty);

            nativePeer.LocalSdpReadytoSend -= CreatOfferCallback;
            nativePeer.LocalSdpReadytoSend += CreatOfferCallback;

            nativePeer.IceCandiateReadytoSend -= IceCandidateReady;
            nativePeer.IceCandiateReadytoSend += IceCandidateReady;

            nativePeer.AddDataChannel();

            nativePeer.CreateOffer();

        }

        public void CreatOfferCallback(string strType, string strOffer)
        {
            Debug.Log($"Offer type: {strType} Offer value : {strOffer}");
        }

        public void IceCandidateReady(string strIceType, int iIceIndex, string strIceCandidate)
        {
            Debug.Log($"ice Type: {strIceType} ice index {iIceIndex} :  ice Value : {strIceCandidate}");
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator WebRtcNativeTestWithEnumeratorPasses()
        {

            IPeer nativePeer1 = new NativePeer(IceServers.Select(i => i.ToString()).ToList(), string.Empty, string.Empty);
            IPeer nativePeer2 = new NativePeer(IceServers.Select(i => i.ToString()).ToList(), string.Empty, string.Empty);

            WebRTCWrapper wrwPeer1 = new WebRTCWrapper(nativePeer1);
            WebRTCWrapper wrwPeer2 = new WebRTCWrapper(nativePeer2);

            //create offer 
            wrwPeer1.MakeOffer();

            Debug.Log("Offer Started waiting for ice");

            //wait for offer to finieh
            while(wrwPeer1.WebRtcObjectState == WebRTCWrapper.State.MakingOffer)
            {
                yield return null;
            }

            Debug.Log($"Offer finished {wrwPeer1.m_tupIceCandidates.Count} ice candidates created");

            //send offer to connection 2
            wrwPeer2.ProcessOfferAndMakeReply(wrwPeer1.m_tupOffer.Item2);

            DateTime now = DateTime.UtcNow;
            while ((DateTime.UtcNow - now).TotalSeconds < 3)
            {
                yield return null;
            }

            Debug.Log("Reply started");

            //send all ice candidates 
            for(int i = 0; i < wrwPeer1.m_tupIceCandidates.Count; i++)
            {
                Debug.Log("processing Ice Candidate sent to peer 2");
                Tuple<string, int, string> ice = wrwPeer1.m_tupIceCandidates[i];
                wrwPeer2.AddICE(ice.Item1, ice.Item2, ice.Item3);
            }

            Debug.Log("Ice Candidate processing finished Reply started, waiting for ice");

            //wait for reply to finish
            while (wrwPeer2.WebRtcObjectState == WebRTCWrapper.State.MakingReply)
            {
                yield return null;
            }

            Debug.Log($"Reply ice finished {wrwPeer2.m_tupIceCandidates.Count} ice candidates created sending answer");

            //send reply back to source 
            wrwPeer1.ProcessReply(wrwPeer2.m_tupReply.Item2);

            now = DateTime.UtcNow;
            while ((DateTime.UtcNow - now).TotalSeconds < 3)
            {
                yield return null;
            }

            Debug.Log("Answer process finished waiting on ice processing");

            //process all the ice candidates
            for(int i = 0; i < wrwPeer2.m_tupIceCandidates.Count; i++)
            {
                Debug.Log("processing Ice Candidate sent to peer 1");
                Tuple<string, int, string> ice = wrwPeer2.m_tupIceCandidates[i];
                wrwPeer1.AddICE(ice.Item1, ice.Item2, ice.Item3);
            }

            Debug.Log("Ice processing finished waiting for connection");

            now = DateTime.UtcNow;
            while ((DateTime.UtcNow - now).TotalSeconds < 5)
            {
                //wait for connection to succeed 
                yield return null;
            }

            //check connection status 
            Debug.Assert(wrwPeer1.WebRtcObjectState == WebRTCWrapper.State.Conncted, "Peer 1 failed to connect");
            Debug.Assert(wrwPeer2.WebRtcObjectState == WebRTCWrapper.State.Conncted, "Peer 2 failed to connect");

            wrwPeer1.SendData("Test 1");
            wrwPeer2.SendData("Test 2");

            now = DateTime.UtcNow;
            while ((DateTime.UtcNow - now).TotalSeconds < 5)
            {
                //wait for message test
                yield return null;
            }


            yield return null;

        }
    }
}
