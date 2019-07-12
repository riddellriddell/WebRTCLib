using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Networking
{
    public class MatchMakingServerSignaling
    {
        public static class MatchMakingServerConstants
        {
            public const string s_strChannelExtension = "";
            public const string s_strLobbyExtension = "";
            public const string s_strSignalSepparationCharacter = "|";
        }

        public class GetLobbyReply
        {
            public bool IsExistingLobby { get; set; }
            public int Id { get; set; }
        }

        public class GetLobyComsReply
        {
            //list of all the communications being sent to the owner of the lobby 
            public List<Tuple<int, string>> Coms { get; set; } = new List<Tuple<int, string>>();
        }

        //used to get or set coms channel data 
        public class UpdateChannel
        {
            public bool Owner { get; set; }
            public bool IsWriteCommand { get; set; }
            public string Data { get; set; }
        }

        public enum Role
        {
            None,
            Negotiating,
            Listener,
            Connector
        }

        public delegate void MessageReceived(int id, string strMessage);

        public event MessageReceived m_evtMessageReceived;

        public Role PeerRole { get; private set; }

        //the lobby to connect to 
        public int? LobbyID { get; private set; } = null;

        //the specific coms channel to communicate through if role is connecting 
        public int? ComsID { get; private set; } = null;

        public ServerConnectionSettings m_scsServerConnectionSettings;

        //start the process for connecting to a match
        public IEnumerator ConnectToMatchMakingServer()
        {
            //indicate trying to connect 
            PeerRole = Role.Negotiating;

            //send request for game 
            var wwwConenctionRequest = UnityWebRequest.Get(m_scsServerConnectionSettings.m_strMatchMakingServerAddress + "data/" + SystemInfo.deviceUniqueIdentifier);

            yield return wwwConenctionRequest.SendWebRequest();

            //reset peer role in case message is returned correctly 
            PeerRole = Role.None;

            //check if listener listening for connections or connector making connections 
            if (!wwwConenctionRequest.isNetworkError && !wwwConenctionRequest.isHttpError)
            {
                var json = wwwConenctionRequest.downloadHandler.text;

                GetLobbyReply glrLobbyReply = JsonUtility.FromJson<GetLobbyReply>(json);

                // if the message is good
                if (glrLobbyReply != null)
                {
                    LobbyID = glrLobbyReply.Id;

                    // depending on what type of message we get, we'll handle it differently
                    // this is the "glue" that allows two peers to establish a connection.
                    if (glrLobbyReply.IsExistingLobby == true)
                    {
                        //connecting to an existing lobby 
                        PeerRole = Role.Connector;
                    }
                    else
                    {
                        //first peer in lobby listen for others joining 
                        PeerRole = Role.Listener;
                    }
                }
            }
        }

        public IEnumerable ListenForComs()
        {
            //get communications 
            if (PeerRole == Role.Connector)
            {
                //check if coms channel has been opened 
                if (ComsID.HasValue == false)
                {
                    yield return null;
                }

                //build the queery 
                UpdateChannel upcChannelUpdate = new UpdateChannel()
                {
                    Owner = true,
                    IsWriteCommand = false,
                    Data = null
                };

                //get the communications channel
                var wwwComsListen = UnityWebRequest.Put(m_scsServerConnectionSettings.m_strMatchMakingServerAddress);

                yield return wwwComsListen.SendWebRequest();

              

                //check if listener listening for connections or connector making connections 
                if (!wwwConenctionRequest.isNetworkError && !wwwConenctionRequest.isHttpError)
                {
                    string strMessage = www.downloadHandler.text;

                    if (string.IsNullOrEmpty(strMessage) == false)
                    {
                        Tuple<int, string> glrLobbyReply = JsonUtility.FromJson<Tuple<int, string>>(strMessage);

                        if (glrLobbyReply != null)
                        {
                            //fire event for message 
                            ProcessRawComsMessage(glrLobbyReply.Item1, glrLobbyReply.Item2);
                        }
                    }
                }
            }
            else if (PeerRole == Role.Listener)
            {
                //check for 
                if(LobbyID.HasValue == false)
                {
                    return;
                }

                //get the communications channel
                var wwwLobbyListen = UnityWebRequest.Put(m_scsServerConnectionSettings.m_strMatchMakingServerAddress);

                //wait for server reply 
                yield return wwwLobbyListen.SendRequest();

                //check if request succeded 
                if (!wwwLobbyListen.isNetworkError && !wwwLobbyListen.isHttpError)
                {
                    string strMessage = www.downloadHandler.text;

                    if (string.IsNullOrEmpty(strMessage) == false)
                    {
                        //get list of all communications sent to owner of lobby 
                        GetLobyComsReply glrLobbyReply = JsonUtility.FromJson<GetLobyComsReply>(json);

                        if (lcdLobbyComs != null)
                        {
                            for(int i = 0; i < glrLobbyReply.Coms.Count; i++)
                            {
                                if(string.IsNullOrWhiteSpace(glrLobbyReply.Coms[i].Item2) == false)
                                {
                                    //fire off message recieved event 
                                    ProcessRawComsMessage(glrLobbyReply.Coms[i].Item1, glrLobbyReply.Coms[i].Item2);
                                }
                            }
                        }
                    }
                }            
            }
        }

        public IEnumerable PostComs(int iComTarget = 0)
        {

        }

        protected void ProcessRawComsMessage(int messageSource, string strRawMessage)
        {
            string[] strMessageList = strRawMessage.Split(strRawMessage, MatchMakingServerConstants.s_strSignalSepparationCharacter);

            foreach (string strIndividualMessage in strMessageList)
            {
                //check for empty
                if (strIndividualMessage != " ")
                {
                    //fire event for message 
                    m_evtMessageReceived(messageSource, strIndividualMessage);
                }
            }
        }
    }
}
