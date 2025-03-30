using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json.Linq;


public class APIManager : MonoBehaviour
{
    //API Key
    [SerializeField] private string apiKey = "";


    private void SetCommonHeaders(UnityWebRequest request)
    {
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
    }

    private void SetCommonHeadersWithoutContentType(UnityWebRequest request)
    {
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);
        request.SetRequestHeader("OpenAI-Beta", "assistants=v2");
    }

    public IEnumerator GetRequest(string url, Action<string> onSuccess, Action<string> onError = null)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        SetCommonHeadersWithoutContentType(request);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke(request.downloadHandler.text);
        }
        else
        {
            onError?.Invoke(request.error + " | " + request.downloadHandler.text);
        }
    }

    public IEnumerator PostRequest(string url, string jsonBody, Action<string> onSuccess, Action<string> onError = null)
    {
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        SetCommonHeaders(request);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            onSuccess?.Invoke(request.downloadHandler.text);
        }
        else
        {
            onError?.Invoke(request.error + " | " + request.downloadHandler.text);
        }
    }
}

