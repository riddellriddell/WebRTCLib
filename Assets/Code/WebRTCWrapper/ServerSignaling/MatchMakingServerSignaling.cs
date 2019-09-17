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
            public const string s_strAPIRouting = "api";
            public const string s_strLobbyRouting = "GameLobbie";
            public const string s_strMessageRouting = "Messages";
            public const string s_strLobbyMatchMakingRouting = "matchmake";
            public const string s_strChannelExtension = "";
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
        
        public ServerConnectionSettings m_scsServerConnectionSettings;

        public delegate void MessageReceived(int id, string strMessage);

        public event MessageReceived m_evtMessageReceived;

        public Queue<Tuple<int, string>> m_messagesRecieved;

        public Role PeerRole { get; private set; } = Role.None;
        
        public GameLobby m_glbGameLobby;

        public PlayerProfile m_plpPlayerProfile;

        public TimeSpan m_tspTimeBetweenServerUpdates = TimeSpan.FromSeconds(2);

        protected DateTime m_dtmTimeOfLastServerCheck = DateTime.MinValue;

        //start the process for connecting to a match
        public IEnumerator ConnectToMatchMakingServer()
        {
            // use existing player profile if it exists else use default and request a new profile from the server
            int iPlayerProfileID = m_plpPlayerProfile == null ? int.MinValue : m_plpPlayerProfile.Id;


            //indicate trying to connect 
            PeerRole = Role.Negotiating;

            //Get game lobby and player profile
            IEnumerator enmTask = GetMatchAndPlayerDetails(iPlayerProfileID);

            while(enmTask.MoveNext())
            {
                yield return null;
            }

            //check if profile and lobby was found
            if(m_plpPlayerProfile == null || m_glbGameLobby == null)
            {
                Debug.LogError("Player profile and game lobby was not found");
                PeerRole = Role.None;
                yield break;
            }

            //check if player is in charge of the lobby 
            if(m_plpPlayerProfile.Id == m_glbGameLobby.Id)
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
            //check if it is time to fetch more messages from the server 
            if(DateTime.UtcNow - m_dtmTimeOfLastServerCheck < m_tspTimeBetweenServerUpdates)
            {
                yield break;
            }

            m_dtmTimeOfLastServerCheck = DateTime.UtcNow;

            // check if player profile is set up
            if(m_plpPlayerProfile == null)
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

            //create address string 
            string strMatchMakingServer = m_scsServerConnectionSettings.m_strMatchMakingServerAddress;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strAPIRouting;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strLobbyRouting;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strLobbyMatchMakingRouting;

            //get the communications channel
            var wwwComsListen = UnityWebRequest.Post(strMatchMakingServer, iPlayerProfileId.ToString());
            wwwComsListen.certificateHandler = new CustomHttpsCert();
            wwwComsListen.SetRequestHeader("Content-Type", "application/json");
            wwwComsListen.timeout = 20;
            yield return wwwComsListen.SendWebRequest();

            //get result 
            if (wwwComsListen.isHttpError || wwwComsListen.isNetworkError)
            {
                string errorType = wwwComsListen.isHttpError ? ("http") : ("net");
                Debug.Log($"error:{errorType} {wwwComsListen.error}");
                //get match failed quit
                yield break;
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

        // try and make a lobby with the target id and attached to the target player id
        // if the lobby already exists the player id will be changed to the new player id 
        public IEnumerator MakeMatchAndPlayerProfile(int iPreferedLobbyID, int iPreferedPlayerId)
        {
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
            wwwComsListen.timeout = 20;
            yield return wwwComsListen.SendWebRequest();

            //get result 
            if (wwwComsListen.isHttpError || wwwComsListen.isNetworkError)
            {
                string errorType = wwwComsListen.isHttpError ? ("http") : ("net");
                Debug.Log($"error:{errorType} {wwwComsListen.error}");
                //get match failed quit
                yield break;
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

        // get messages for player from server
        public IEnumerator GetMessages(int iPlayerID)
        {
            //create address string 
            string strMatchMakingServer = m_scsServerConnectionSettings.m_strMatchMakingServerAddress;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strAPIRouting;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strMessageRouting;
            strMatchMakingServer += "/" + iPlayerID.ToString();

            //get the communications channel
            var wwwComsListen = UnityWebRequest.Post(strMatchMakingServer, "");
            wwwComsListen.certificateHandler = new CustomHttpsCert();
            wwwComsListen.SetRequestHeader("Content-Type", "application/json");
            wwwComsListen.timeout = 20;
            yield return wwwComsListen.SendWebRequest();

            //get result 
            if (wwwComsListen.isHttpError || wwwComsListen.isNetworkError)
            {
                string errorType = wwwComsListen.isHttpError ? ("http") : ("net");
                Debug.Log($"error:{errorType} {wwwComsListen.error}");
                //get match failed quit
                yield break;
            }

            Debug.Log("Result:" + wwwComsListen.downloadHandler.text);

            try
            {
                List<Tuple<int, string>> tupMessagesFromServer = JsonUtility.FromJson<List<Tuple<int, string>>>(wwwComsListen.downloadHandler.text);
                for (int i = 0; i < tupMessagesFromServer.Count; i++)
                {
                    ProcessRawComsMessage(tupMessagesFromServer[i].Item1, tupMessagesFromServer[i].Item2);
                }
            }
            catch
            {
                Debug.Log("Error deserializing server response");
            }

        }

        // send message to taget player
        public IEnumerator SendMessage(int iFromPlayerID, int iToPlayerID, string strMessage)
        {
            //create address string 
            string strMatchMakingServer = m_scsServerConnectionSettings.m_strMatchMakingServerAddress;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strAPIRouting;
            strMatchMakingServer += "/" + MatchMakingServerConstants.s_strMessageRouting;

            string strJson = JsonUtility.ToJson(new Tuple<int, int, string>(iFromPlayerID, iToPlayerID, strMessage));

            //get the communications channel
            var wwwComsListen = UnityWebRequest.Post(strMatchMakingServer, strJson);
            wwwComsListen.certificateHandler = new CustomHttpsCert();
            wwwComsListen.SetRequestHeader("Content-Type", "application/json");
            wwwComsListen.timeout = 20;
            yield return wwwComsListen.SendWebRequest();

            //get result 
            if (wwwComsListen.isHttpError || wwwComsListen.isNetworkError)
            {
                string errorType = wwwComsListen.isHttpError ? ("http") : ("net");
                Debug.Log($"error:{errorType} {wwwComsListen.error}");
                //get match failed quit
                yield break;
            }

            Debug.Log("Result:" + wwwComsListen.downloadHandler.text);

        }

        //process messages and fire events
        protected void ProcessRawComsMessage(int messageSource, string strMessage)
        {
            //check for empty
            if (string.IsNullOrWhiteSpace(strMessage) == false)
            {
                //store message 
                m_messagesRecieved.Enqueue(new Tuple<int, string>(messageSource, strMessage));

                //fire event for message 
                m_evtMessageReceived(messageSource, strMessage);
            }
        }
    }
}
