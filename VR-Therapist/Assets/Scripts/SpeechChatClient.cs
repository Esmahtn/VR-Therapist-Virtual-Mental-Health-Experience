using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using UnityEngine.InputSystem; // Yeni Input System için
using UnityEngine.XR.Interaction.Toolkit; // VR etkileşimi için

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

    [Header("VR Giriş Ayarları")]
    [Tooltip("Sol el X butonu için: XRI LeftHand Interaction/Select veya Primary Button seçebilirsin")]
    public InputActionProperty vrRecordAction; 

    private string microphoneDevice;
    private AudioClip recording;
    private bool isRecording = false;

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        // Mikrofona erişim kontrolü
        if (Microphone.devices.Length > 0) 
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log("Mikrofon Hazır: " + microphoneDevice);
        }
        else
        {
            Debug.LogError("Mikrofon bulunamadı!");
        }
    }

    void Update()
    {
        // VR Butonuna basıldığı an (Kayıt Başlat)
        if (vrRecordAction.action.WasPressedThisFrame() && !isRecording && microphoneDevice != null) 
        {
            StartRecording();
        }

        // VR Butonu bırakıldığı an (Kaydı Durdur ve Gönder)
        if (vrRecordAction.action.WasReleasedThisFrame() && isRecording) 
        {
            StopRecordingAndProcess();
        }
    }

    void StartRecording()
    {
        isRecording = true;
        // 16000 Hz genellikle STT servisleri için idealdir
        recording = Microphone.Start(microphoneDevice, false, (int)maxRecordingDuration, 16000);
        Debug.Log("🔊 VR Kayıt Başladı...");
    }

    void StopRecordingAndProcess()
    {
        if (!isRecording) return;
        isRecording = false;

        int position = Microphone.GetPosition(microphoneDevice); 
        Microphone.End(microphoneDevice);

        if (position < 1600) // 0.1 saniyeden kısa kayıtları reddet
        {
            Debug.LogWarning("Kayıt çok kısa, işlem iptal edildi.");
            return;
        }

        AudioClip finalClip = TrimAudioClip(recording, position, recording.channels, recording.frequency);
        Debug.Log("🛑 VR Kayıt Durduruldu. Server'a gönderiliyor...");
        
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
        
        if (string.IsNullOrEmpty(userText)) 
        { 
            Debug.LogError("STT başarısız veya boş döndü.");
            Destroy(recordedClip); 
            yield break; 
        }
        Debug.Log("Sen: " + userText);

        // 2. CHAT (Gemini İşleme)
        string geminiReply = "";
        yield return StartCoroutine(SendChatRequest(userText, result => geminiReply = result));
        Debug.Log("Terapist: " + geminiReply);

        // 3. TTS (Sese Çevir ve Oynat)
        if (!string.IsNullOrEmpty(geminiReply)) 
        {
            yield return StartCoroutine(SendTtsRequest(geminiReply));
        }

        Destroy(recordedClip);
    }

    IEnumerator SendSTTRequest(AudioClip clip, System.Action<string> callback)
    {
        string url = BASE_URL + "/stt";
        byte[] wavBytes = WavUtility.FromAudioClip(clip); // WavUtility dosyası projenizde olmalı
        
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBytes, "recording.wav", "audio/wav");
        
        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
            callback(JsonUtility.FromJson<STTResponse>(request.downloadHandler.text).text);
        else 
        {
            Debug.LogError("STT Hatası: " + request.error);
            callback("");
        }
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
        else 
        {
            Debug.LogError("Chat Hatası: " + request.error);
            callback("");
        }
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
        else
        {
            Debug.LogError("TTS Hatası: " + request.error);
        }
    }
}