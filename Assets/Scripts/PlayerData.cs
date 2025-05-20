using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Net;

public class PlayerData : MonoBehaviour
{
    public static PlayerData instance;

    public string userID;
    public string nickname;
    public int selectedCharacterIndex;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);  // 씬 이동해도 유지
        }
        else
        {
            Destroy(gameObject);    // 중복 방지
        }
    }

    public void StartKeepAlive()
    {
        StartCoroutine(SendKeepAliveLoop());
    }
        IEnumerator SendKeepAliveLoop()
    {
        while (true)
        {
            var data = new { username = userID };
            string json = JsonUtility.ToJson(data);

            UnityWebRequest req = new UnityWebRequest("http://43.203.206.157:3000/keep-alive", "POST");
            byte[] jsonBytes = new System.Text.UTF8Encoding().GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(jsonBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Keep-alive 실패: " + req.error);
            }

            yield return new WaitForSeconds(60f);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
