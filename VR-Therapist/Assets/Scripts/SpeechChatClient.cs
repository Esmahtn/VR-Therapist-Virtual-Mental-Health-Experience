using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

[System.Serializable] public class ChatResponse { public string reply; }
[System.Serializable] public class ChatRequestData { public string text; public string context; }
[System.Serializable] public class STTResponse { public string text; }
[System.Serializable] public class TtsRequestData { public string text; }

public class SpeechChatClient : MonoBehaviour
{
    private const string BASE_URL = "http://127.0.0.1:5001"; 

    [Header("Ayarlar")]
    public float maxRecordingDuration = 10f;
    public AudioSource audioSource;

    private string microphoneDevice;
    private AudioClip recording;
    private bool isRecording = false;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (Microphone.devices.Length > 0) microphoneDevice = Microphone.devices[0];
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
    {
        Debug.Log("KLAVYEDEN SPACE TUŞUNA BASILDI!");
    }
        if (Input.GetKeyDown(KeyCode.Space) && !isRecording && microphoneDevice != null) StartRecording();
        if (Input.GetKeyUp(KeyCode.Space) && isRecording) StopRecordingAndProcess();
    }

    void StartRecording()
    {
        isRecording = true;
        recording = Microphone.Start(microphoneDevice, false, (int)maxRecordingDuration, 16000);
        Debug.Log("🔊 Kayıt Başladı...");
    }

    void StopRecordingAndProcess()
    {
        if (!isRecording) return;
        isRecording = false;

        // 1. ÖNCE değişkeni tanımla ve mikrofonun o anki yerini al
        int position = Microphone.GetPosition(microphoneDevice); 

        // 2. SONRA mikrofonu durdur
        Microphone.End(microphoneDevice);

        // 3. ŞİMDİ o değişkeni kontrol et (Hata aldığın yer burasıydı)
        if (position < 1600) 
        {
            Debug.LogWarning("Kayıt çok kısa (0.1 sn altı), işlem iptal edildi.");
            return;
        }

        // 4. Kalan işlemler
        AudioClip finalClip = TrimAudioClip(recording, position, recording.channels, recording.frequency);
        Debug.Log("🛑 Kayıt Durduruldu. İşlem Başlatılıyor...");
        
        StartCoroutine(FullChatCycle(finalClip));
    }
    private AudioClip TrimAudioClip(AudioClip originalClip, int samplesEnd, int channels, int frequency)
    {
        float[] samples = new float[samplesEnd * channels];
        originalClip.GetData(samples, 0);
        AudioClip newClip = AudioClip.Create("Trimmed", samplesEnd, channels, frequency, false);
        newClip.SetData(samples, 0);
        return newClip;
    }

    IEnumerator FullChatCycle(AudioClip recordedClip)
    {
        // 1. STT (Sesi Metne Çevir)
        string userText = "";
        yield return StartCoroutine(SendSTTRequest(recordedClip, result => userText = result));
        if (string.IsNullOrEmpty(userText)) { Destroy(recordedClip); yield break; }
        Debug.Log("Söylenen: " + userText);

        // 2. CHAT (Zeka - Gemini)
        string geminiReply = "";
        yield return StartCoroutine(SendChatRequest(userText, result => geminiReply = result));
        Debug.Log("Gelen Cevap: " + geminiReply);

        // 3. TTS (Metni Sese Çevir ve Oynat)
        if (!string.IsNullOrEmpty(geminiReply)) yield return StartCoroutine(SendTtsRequest(geminiReply));

        Destroy(recordedClip);
    }

    IEnumerator SendSTTRequest(AudioClip clip, System.Action<string> callback)
    {
        string url = BASE_URL + "/stt";
        byte[] wavBytes = WavUtility.FromAudioClip(clip); // WavUtility scripti Asset içinde olmalı
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBytes, "recording.wav", "audio/wav");
        
        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
            callback(JsonUtility.FromJson<STTResponse>(request.downloadHandler.text).text);
        else callback("");
    }

    IEnumerator SendChatRequest(string text, System.Action<string> callback)
    {
        string url = BASE_URL + "/chat";
        string json = JsonUtility.ToJson(new ChatRequestData { text = text });
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
            callback(JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text).reply);
        else callback("");
    }

    IEnumerator SendTtsRequest(string text)
    {
        string url = BASE_URL + "/tts";
        string json = JsonUtility.ToJson(new TtsRequestData { text = text });
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] body = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(body);
        request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            audioSource.clip = DownloadHandlerAudioClip.GetContent(request);
            audioSource.Play();
            while (audioSource.isPlaying) yield return null;
        }
    }
}