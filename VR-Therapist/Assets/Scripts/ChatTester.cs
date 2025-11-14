using UnityEngine;

public class ChatTester : MonoBehaviour
{
    public ChatAPI api;

    void Start()
    {
        StartCoroutine(api.SendChatRequest("Unity'den selam!", (reply) =>
        {
            Debug.Log("TERAPIST: " + reply);
        }));
    }
}
