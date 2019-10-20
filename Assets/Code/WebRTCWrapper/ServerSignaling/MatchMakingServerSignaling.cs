using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

namespace Networking
{
    public class MatchMakingServerSignaling
    {
        public static class MatchMakingServerConstants
        {
            public const string s_strAPIRouting = "api";
            public const string s_strLobbyRouting = "GameLobby";
            public const string s_strMessageRouting = "Messages";
            public const string s_strLobbyMatchMakingRouting = "matchmake";
            public const string s_strChannelExtension = "";
            public const string s_strSignalSepparationCharacter = "|";
        }

        public enum Role
        {
            None,
            Negotiating,
            Listener,
            Connector
        }

        public ServerConnectionSettings m_scsServerConnectionSettings;

        public delegate void MessageReceived(int id, string strMessage);

        public event MessageReceived m_evtMessageReceived;

        public Queue<Tuple<int, string>> m_messagesRecieved = new Queue<Tuple<int, string>>();

        public Role PeerRole { get; private set; } = Role.None;

        public GameLobby m_glbGameLobby;

        public PlayerProfile m_plpPlayerProfile;

        protected TimeSpan m_tspTimeBetweenMessages = TimeSpan.FromSeconds(1);

        protected DateTime m_dtmTimeOfLastMessage = DateTime.MinValue;

        protected TimeSpan m_tspTimeBetweenMessageGet = TimeSpan.FromSeconds(4);

        protected DateTime m_dtmTimeOfLastMessageGet = DateTime.MinValue;

        protected TimeSpan m_tspTimeBetweenServerUpdate = TimeSpan.FromSeconds(4);

        protected DateTime m_dtmTimeOfLastServerUpdate = DateTime.MinValue;

        public bool TimeForNextMessage()
        {
            //check if it is time to fetch more messages from the server 
            if ((DateTime.UtcNow - m_dtmTimeOfLastMessage) < m_tspTimeBetweenMessages)
            {
                return false;
            }
            
            m_dtmTimeOfLastMessage = DateTime.UtcNow;
            
            return true;
        }

        public bool TimeForServerUpdate()
        {
            //check if it is time to update the server state on the server
            if ((DateTime.UtcNow - m_dtmTimeOfLastServerUpdate) < m_tspTimeBetweenServerUpdate)
            {
                return false;
            }

            m_dtmTimeOfLastServerUpdate = DateTime.UtcNow;

            return true;
        }

        //start the process for connecting to a match
        public IEnumerator ConnectToMatchMakingServer()
        {
            //add random delay to avoid race condittions 
            yield return new WaitForSeconds(Random.Range(0, 5));

            // use existing player profile if it exists else use default and request a new profile from the server
            int iPlayerProfileID = m_plpPlayerProfile == null ? int.MinValue : m_plpPlayerProfile.Id;

            //indicate trying to connect 
            PeerRole = Role.Negotiating;

            //Get game lobby and player profile
            IEnumerator enmTask = GetMatchAndPlayerDetails(iPlayerProfileID);

            while (enmTask.MoveNext())
            {
                yield return null;
            }

            //check if profile and lobby was found
            if (m_plpPlayerProfile == null || m_glbGameLobby == null)
            {
                Debug.LogError("Player profile and game lobby was not found");
                PeerRole = Role.None;
                yield break;
            }

            //check if player is in charge of the lobby 
            if (m_plpPlayerProfile.Id == m_glbGameLobby.OwnerId)
            {
                PeerRole = Role.Listener;
            }
            else
            {
                PeerRole = Role.Connector;
            }
        }

        public void SetupDefaultNetworkingSettings()
        {
            if (m_scsServerConnectionSettings == null)
            {
                m_scsServerConnectionSettings = ScriptableObject.CreateInstance<ServerConnectionSettings>();

                m_scsServerConnectionSettings.m_strMatchMakingServerAddress = "https://localhost:44322";
            }
        }

        public IEnumerator UpdateMessages()
        {

            if (DateTime.UtcNow - m_dtmTimeOfLastMessageGet < m_tspTimeBetweenMessageGet)
            {
                yield break;
            }

            m_dtmTimeOfLastMessageGet = DateTime.UtcNow;


            // check if player profile is set up
            if (m_plpPlayerProfile == null)
            {
                yield break;
            }

            yield return GetMessages(m_plpPlayerProfile.Id);

        }

        //try and get player profile from server 
        public IEnumerator GetPlayerProfile()
        {
            yield break;
        }

        // ask the server to find a lobby using the prefered player id
        // if the prefered player id does not exists a new player is created and returned
        // if no non full lobbies exist a new lobby is created
        public IEnumerator GetMatchAndPlayerDetails(int iPlayerProfileId)
        {
            //delay untill its time for next message
            while (TimeForNextMessage() == false)
            {
                yield return null;
            }

            //create address string 
            string strMatchMakingServer = m_scsServerConnectionSettings.m_strMatchMakingServerAddress;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strAPIRouting;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strLobbyRouting;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strLobbyMatchMakingRouting;

            //get the communications channel
            var wwwComsListen = UnityWebRequest.Post(strMatchMakingServer, iPlayerProfileId.ToString());
            wwwComsListen.certificateHandler = new CustomHttpsCert();
            wwwComsListen.SetRequestHeader("Content-Type", "application/json");
            wwwComsListen.timeout = 5;

            yield return wwwComsListen.SendWebRequest();

            //get result 
            if (wwwComsListen.isHttpError || wwwComsListen.isNetworkError)
            {
                string errorType = wwwComsListen.isHttpError ? ("http") : ("net");
                Debug.Log($"error:{errorType} {wwwComsListen.error}");
                //get match failed quit
                yield break;
            }

            while (wwwComsListen.isDone == false)
            {
                yield return null;
            }

            Debug.Log("Result:" + wwwComsListen.downloadHandler.text);

            try
            {
                CreateLobbyClass mgtServerResponse = JsonUtility.FromJson<CreateLobbyClass>(wwwComsListen.downloadHandler.text);
                m_glbGameLobby = mgtServerResponse.gameLobby;
                m_plpPlayerProfile = mgtServerResponse.playerProfile;
            }
            catch
            {
                Debug.Log("Error deserializing server response");
            }

        }

        // try and make a lobby with the target id and attached to the target player id
        // if the lobby already exists the player id will be changed to the new player id 
        public IEnumerator MakeMatchAndPlayerProfile(int iPreferedLobbyID, int iPreferedPlayerId)
        {
            //delay until its time for next message
            while (TimeForNextMessage() == false)
            {
                yield return null;
            }

            //create address string 
            string strMatchMakingServer = m_scsServerConnectionSettings.m_strMatchMakingServerAddress;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strAPIRouting;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strLobbyRouting;

            //create json string 
            string strJson = JsonUtility.ToJson(new Tuple<int, int>(iPreferedLobbyID, iPreferedPlayerId));

            //get the communications channel
            var wwwComsListen = UnityWebRequest.Post(strMatchMakingServer, strJson);
            wwwComsListen.certificateHandler = new CustomHttpsCert();
            wwwComsListen.SetRequestHeader("Content-Type", "application/json");
            wwwComsListen.timeout = 5;
            yield return wwwComsListen.SendWebRequest();

            //get result 
            if (wwwComsListen.isHttpError || wwwComsListen.isNetworkError)
            {
                string errorType = wwwComsListen.isHttpError ? ("http") : ("net");
                Debug.Log($"error:{errorType} {wwwComsListen.error}");
                //get match failed quit
                yield break;
            }

            while (wwwComsListen.isDone == false)
            {
                yield return null;
            }

            Debug.Log("Result:" + wwwComsListen.downloadHandler.text);

            try
            {
                Tuple<GameLobby, PlayerProfile> tupServerResponse = JsonUtility.FromJson<Tuple<GameLobby, PlayerProfile>>(wwwComsListen.downloadHandler.text);
                m_glbGameLobby = tupServerResponse.Item1;
                m_plpPlayerProfile = tupServerResponse.Item2;
            }
            catch
            {
                Debug.Log("Error deserializing server response");
            }
        }

        public IEnumerator UpdateGameLobby(int iLobbyIndex, int iPlayerCount, int iGameState)
        {
            //check if it is time to update the server
            if(TimeForServerUpdate() == false)
            {
                yield break;
            }

            //delay untill its time for next message
            while (TimeForNextMessage() == false)
            {
                yield return null;
            }

            //create address string 
            string strMatchMakingServer = m_scsServerConnectionSettings.m_strMatchMakingServerAddress;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strAPIRouting;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strLobbyRouting;

            //create source data object
            UpdateLobby uplUpdate = new UpdateLobby()
            {
                id = iLobbyIndex,
                playerCount = iPlayerCount,
                state = iGameState
            };

            //create json string 
            string strJson = JsonUtility.ToJson(uplUpdate);

            //get the communications channel
            var wwwComsListen = UnityWebRequest.Put(strMatchMakingServer, strJson);
            wwwComsListen.certificateHandler = new CustomHttpsCert();
            wwwComsListen.SetRequestHeader("Content-Type", "application/json");
            wwwComsListen.timeout = 5;
            yield return wwwComsListen.SendWebRequest();

            //get result 
            if (wwwComsListen.isHttpError || wwwComsListen.isNetworkError)
            {
                string errorType = wwwComsListen.isHttpError ? ("http") : ("net");
                Debug.Log($"error:{errorType} {wwwComsListen.error}");
                //get match failed quit
                yield break;
            }

            while (wwwComsListen.isDone == false)
            {
                yield return null;
            }

            Debug.Log("Result:" + wwwComsListen.downloadHandler.text);
        }

        // get messages for player from server
        public IEnumerator GetMessages(int iPlayerID)
        {

            //create address string 
            string strMatchMakingServer = m_scsServerConnectionSettings.m_strMatchMakingServerAddress;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strAPIRouting;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strMessageRouting;
            strMatchMakingServer += "/" + iPlayerID.ToString();

            //get the communications channel
            var wwwComsListen = UnityWebRequest.Put(strMatchMakingServer, iPlayerID.ToString());
            wwwComsListen.certificateHandler = new CustomHttpsCert();
            wwwComsListen.SetRequestHeader("Content-Type", "application/json");
            wwwComsListen.timeout = 5;
            yield return wwwComsListen.SendWebRequest();

            //get result 
            if (wwwComsListen.isHttpError || wwwComsListen.isNetworkError)
            {
                string errorType = wwwComsListen.isHttpError ? ("http") : ("net");
                Debug.Log($"Get Message for {iPlayerID.ToString()} error:{errorType} {wwwComsListen.error}");
                //get match failed quit
                yield break;
            }

            while (wwwComsListen.isDone == false)
            {
                yield return null;
            }

            Debug.Log($"Get Message for {iPlayerID.ToString()} Result: {wwwComsListen.downloadHandler.text}");

            //check if there was any messages
            if (string.IsNullOrEmpty(wwwComsListen.downloadHandler.text))
            {
                yield break;
            }

            try
            {
                MessageGet tupMessagesFromServer = JsonUtility.FromJson<MessageGet>(wwwComsListen.downloadHandler.text);
                for (int i = 0; i < tupMessagesFromServer.messages.Count; i++)
                {
                    ProcessRawComsMessage(tupMessagesFromServer.messages[i]);
                }
            }
            catch
            {
                Debug.Log($"Get Message for {iPlayerID.ToString()} Error deserializing server response");
            }

        }

        // send message to taget player
        public IEnumerator SendMessage(int iFromPlayerID, int iToPlayerID, string strMessage)
        {
            //delay untill its time for next message
            while (TimeForNextMessage() == false)
            {
                yield return null;
            }

            //create address string 
            string strMatchMakingServer = m_scsServerConnectionSettings.m_strMatchMakingServerAddress;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strAPIRouting;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strMessageRouting;

            string strJson = JsonUtility.ToJson(new MessageSend() { fromId = iFromPlayerID, toId = iToPlayerID, message = strMessage });

            Debug.Log($"Sending Message {strJson} to {strMatchMakingServer}");

            //get the communications channel
            var wwwComsListen = UnityWebRequest.Put(strMatchMakingServer, strJson);
            wwwComsListen.method = "POST";
            wwwComsListen.certificateHandler = new CustomHttpsCert();
            wwwComsListen.SetRequestHeader("Content-Type", "application/json");
           
            //wwwComsListen.timeout = 20;
            yield return wwwComsListen.SendWebRequest();

            //get result 
            if (wwwComsListen.isHttpError || wwwComsListen.isNetworkError)
            {
                string errorType = wwwComsListen.isHttpError ? ("http") : ("net");
                Debug.Log($"error:{errorType} {wwwComsListen.error}");
                //get match failed quit
                yield break;
            }

            while (wwwComsListen.isDone == false)
            {
                yield return null;
            }

            Debug.Log("Result:" + wwwComsListen.downloadHandler.text);

        }

        //process messages and fire events
        protected void ProcessRawComsMessage(Message msgMessage)
        {
            //check for empty
            if (string.IsNullOrWhiteSpace(msgMessage.Value) == false)
            {
                Debug.Log($"Message received from: {msgMessage.Value} with value: {msgMessage.Value}");

                //store message 
                m_messagesRecieved.Enqueue(new Tuple<int, string>(msgMessage.FromPlayerProfileId, msgMessage.Value));

                //fire event for message 
                m_evtMessageReceived?.Invoke(msgMessage.Id, msgMessage.Value);
            }
        }
    }
}
