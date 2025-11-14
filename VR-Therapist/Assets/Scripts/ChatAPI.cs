using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ChatAPI : MonoBehaviour
{
    public string serverUrl = "http://127.0.0.1:5001/chat";

    [System.Serializable]
    public class ChatRequest
    {
        public string text;
        public string context;
    }

    [System.Serializable]
    public class ChatResponse
    {
        public string reply;
    }

    public IEnumerator SendChatRequest(string userMessage, System.Action<string> callback)
    {
        ChatRequest req = new ChatRequest
        {
            text = userMessage,
            context = ""
        };

        string jsonData = JsonUtility.ToJson(req);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest www = new UnityWebRequest(serverUrl, "POST");
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("SERVER RESPONSE RAW: " + www.downloadHandler.text);

            ChatResponse resp = JsonUtility.FromJson<ChatResponse>(www.downloadHandler.text);
            callback?.Invoke(resp.reply);
        }
        else
        {
            Debug.LogError("CHAT API ERROR: " + www.error);
            callback?.Invoke("Server error");
        }
    }
}
