using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class SignallingServerTest
    {
        public class TestConnectionClass
        {
            protected const string c_strOffer = "Offer";
            protected const string c_strReply = "Reply";
            protected const string c_strIceCandidate = "IceCandidate";

            protected const float c_fOfferTime = 1;
            protected const float c_fReplyTime = 1;
            protected const float c_fIceFormulationTime = 1;
            protected const int c_iIceNumber = 3;

            public Action<string> m_evtOfferReady;
            public Action<string> m_evtReplyReady;
            public Action<string> m_evtIceReady;

            protected float m_fTimeUntilOfferReady = -1;
            protected float m_fTimeUntilReplyReady = -1;
            protected float m_fTimeUntilNextIce =-1;
            protected int m_iIceRemaining = 0;

            protected string m_strReceivedOffer = string.Empty;
            protected string m_strReceivedReply = string.Empty;
            protected List<string> m_strReceivedIce = new List<string>();

            public void Update(float fDeltaTime )
            {
                //check if it is time to send offer
                if(m_fTimeUntilOfferReady > 0)
                {
                    m_fTimeUntilOfferReady -= fDeltaTime;

                    if(m_fTimeUntilOfferReady <= 0)
                    {
                        m_evtOfferReady?.Invoke(c_strOffer);
                    }
                }

                //check if it is time to send reply
                if (m_fTimeUntilReplyReady > 0)
                {
                    m_fTimeUntilReplyReady -= fDeltaTime;

                    if (m_fTimeUntilReplyReady <= 0)
                    {
                        m_evtReplyReady?.Invoke(c_strOffer);
                    }
                }

                //check if it is time to send ice
                if (m_fTimeUntilNextIce > 0)
                {
                    m_fTimeUntilNextIce -= fDeltaTime;

                    if (m_fTimeUntilNextIce <= 0)
                    {
                        m_evtIceReady?.Invoke(c_strOffer);

                        if(--m_iIceRemaining > 0)
                        {
                            m_fTimeUntilNextIce = c_fIceFormulationTime;
                        }
                    }
                }

            }

            public void GetOffer()
            {
                m_strReceivedOffer = c_strOffer;
                m_fTimeUntilOfferReady = c_fOfferTime;
            }

            public void ProcessOffer(string strOffer)
            {
                m_strReceivedOffer = strOffer;
                m_strReceivedReply = c_strReply;

                m_fTimeUntilReplyReady = c_fReplyTime;
                m_fTimeUntilNextIce = c_fIceFormulationTime;
                m_iIceRemaining = c_iIceNumber;
            }

            public void ProcessReply(string strReply)
            {
                m_strReceivedReply = strReply;
            }

            public void ProcessIceCandidate(string strIceCandidate)
            {
                m_strReceivedIce.Add(strIceCandidate);
            }

            protected void CheckIfDone()
            {
                if(string.IsNullOrWhiteSpace(m_strReceivedOffer))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(m_strReceivedReply))
                {
                    return;
                }

                if (m_strReceivedIce.Count < c_iIceNumber)
                {
                    return;
                }
            }

            bool finished;
        }

        // A Test behaves as an ordinary method
        [Test]
        public void SignallingServerTestSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator SignallingServerTestWithEnumeratorPasses()
        {
            // Use the Assert class to test conditions.
            // Use yield to skip a frame.
            yield return null;
        }

        public IEnumerator CreateConnectionTest()
        {
            //get game lobby
           
            //if listener pole for communications 

            //loop through all active connections

            //check if connection has ice candidate 

            //check if connection has offer

            //on communication 

            //check if connecion exists for coms target

            //create rtc connection

            //check if communication is offer or ice 

            //apply offer to rtc

            //apply ice to rtc 
        }

        public IEnumerator CreateConnection()
        {

        }

        public IEnumerator ListenForConnections()
        {

        }
    }
}
