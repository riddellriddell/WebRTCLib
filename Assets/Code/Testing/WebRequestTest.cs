using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Networking;

public class WebRequestTest : MonoBehaviour
{
    public string m_Address = "https://localhost:44322/api/GameLobbie"; 

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WebRequestCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator WebRequestCoroutine()
    {
        //get the communications channel
        var wwwComsListen = UnityWebRequest.Get(m_Address);
        wwwComsListen.certificateHandler = new CustomHttpsCert();
        //wwwComsListen.SetRequestHeader("Content-Type", "application/json");
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


        Debug.Log("Result:" + wwwComsListen.downloadHandler.text);

    }
}
