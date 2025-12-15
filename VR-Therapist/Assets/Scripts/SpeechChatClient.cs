using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

// Sahne geçişi için gerekli, bunu eklemeyi unutmayın!
using UnityEngine.SceneManagement;

// JSON'dan gelen yanıtları tutmak için yardımcı sınıflar
[System.Serializable]
public class ChatResponse { public string reply; }

[System.Serializable]
public class ChatRequestData // /chat'e gönderilecek veri yapısı
{
    public string text;
    public string context;
}

[System.Serializable]
public class STTResponse { public string text; }

[System.Serializable]
public class TtsRequestData // /tts'e gönderilecek veri yapısı
{
    public string text;
}


public class SpeechChatClient : MonoBehaviour
{
    
    // Lütfen server'ın adresini kontrol edin. Eğer server farklı bir IP'de ise burayı güncelleyin.
    private const string BASE_URL = "http://127.0.0.1:5001"; 

    [Header("Ayarlar")]
    public float maxRecordingDuration = 10f; // Saniyelik maksimum kayıt süresi
    
    [Header("Gerekli Referanslar")]
    public AudioSource audioSource; // Sahnenize bir AudioSource ekleyin ve buraya sürükleyin.

    private string microphoneDevice;
    private AudioClip recording;
    private bool isRecording = false;

    void Start()
    {
        // 1. AudioSource kontrolü
        if (audioSource == null)
        {
            Debug.LogError("AudioSource referansı eksik! Lütfen bir AudioSource ekleyin.");
            return;
        }

        // 2. Mikrofon cihazı tespiti
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log($"Kullanılan Mikrofon: {microphoneDevice}");
        }
        else
        {
            Debug.LogError("Mikrofon bulunamadı! Lütfen bir mikrofon bağlayın.");
        }
    }

    void Update()
    {
        // Space tuşu basılı tutulunca kaydı başlat
        if (Input.GetKeyDown(KeyCode.Space) && !isRecording && microphoneDevice != null)
        {
            StartRecording();
        }

        // Space tuşu bırakılınca kaydı durdur ve süreci başlat
        if (Input.GetKeyUp(KeyCode.Space) && isRecording)
        {
            StopRecordingAndProcess();
        }
    }

    // --- Kayıt İşlemleri ---
    void StartRecording()
    {
        if (isRecording) return;
        isRecording = true;
        Debug.Log("🔊 Kayıt Başlatıldı...");
        // Mikrofonu 16000 Hz örnekleme hızıyla başlatmak Google STT için idealdir.
        recording = Microphone.Start(microphoneDevice, false, (int)maxRecordingDuration, 16000);
    }

    void StopRecordingAndProcess()
    {
        if (!isRecording) return;
        isRecording = false;
        
        // Kaydı durdur, ancak mikrofonun son pozisyonunu al.
        int position = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);

        if (position <= 0)
        {
            Debug.LogWarning("Boş veya çok kısa kayıt. İşlem iptal edildi.");
            return;
        }

        // Kesilen kaydı oluştur (Başlangıçta 10 saniyelik buffer alınmıştır)
        AudioClip finalClip = TrimAudioClip(recording, position, recording.channels, recording.frequency);
        Destroy(recording); // Eski clip'i bellekten kaldır.

        Debug.Log("🛑 Kayıt Durduruldu. İşlem Başlatılıyor...");
        
        // Ana süreci başlat: STT -> Chat -> TTS
        StartCoroutine(FullChatCycle(finalClip));
    }

    // Ses kaydının fazlalığını kesme (Trim) fonksiyonu
    private AudioClip TrimAudioClip(AudioClip originalClip, int samplesEnd, int channels, int frequency)
    {
        float[] samples = new float[samplesEnd * channels];
        originalClip.GetData(samples, 0);

        AudioClip newClip = AudioClip.Create("TrimmedRecording", samplesEnd, channels, frequency, false);
        newClip.SetData(samples, 0);
        return newClip;
    }

    // --- Ana İletişim Döngüsü ---
    IEnumerator FullChatCycle(AudioClip recordedClip)
    {
        string userText = "Konuşma algılanamadı.";
        
        // 1. STT: Kaydı Metne Çevir
        Debug.Log("1. 🎙️ Kayıt STT'ye Gönderiliyor...");
        yield return StartCoroutine(SendSTTRequest(recordedClip, result => userText = result));

        Debug.Log($"   -> Kullanıcı Dedi: **{userText}**");
        
        if (string.IsNullOrEmpty(userText) || userText == "Konuşma algılanamadı.")
        {
            Debug.LogWarning("STT boş döndü veya hata oluştu. İşlem durduruldu.");
            Destroy(recordedClip);
            yield break;
        }

        // 2. CHAT: Metni Gemini'ye Gönder
        string geminiReply = "";
        Debug.Log("2. 🤖 Metin Gemini Chat'e Gönderiliyor...");
        yield return StartCoroutine(SendChatRequest(userText, result => geminiReply = result));

        Debug.Log($"   -> Gemini Yanıtı: **{geminiReply}**");

        if (string.IsNullOrEmpty(geminiReply))
        {
            Debug.LogError("Gemini'den yanıt alınamadı. İşlem durduruldu.");
            Destroy(recordedClip);
            yield break;
        }

        // <<< YÖNLENDİRME KONTROLÜ VE ÖZEL YÖNLENDİRME MESAJI (KRİTİK KISIM) >>>
        if (geminiReply.Trim() == "[REDIRECT_RAGE_ROOM]")
        {
            // Kullanıcıyı yönlendirme komutu alındı
            Debug.LogWarning("🚨 YÖNLENDİRME KOMUTU ALINDI: Stres Atma Odasına geçiliyor...");
            
            // YÖNLENDİRME İÇİN DÜZGÜN BİR SESLİ YANIT ÜRET
            string redirectText = "Anlıyorum. Harika bir fikir! Haydi stresini atabileceğin odaya geçelim.";
            
            // TTS BİTENE KADAR BEKLE
            yield return StartCoroutine(SendTtsRequest(redirectText)); 
            
            // TTS BİTTİĞİNDE: Artık güvenle sahne geçişi yapabiliriz.
            Debug.Log("TTS oynatma bitti. Sahne geçişi yapılıyor...");
            SceneManager.LoadScene("TaskRoom");
            
            Destroy(recordedClip);
            yield break; // Döngüyü burada BİTİR.
        }
        // <<< YÖNLENDİRME KONTROLÜ BİTTİ >>>


        // 3. TTS: Yanıtı Sese Çevir ve Oynat (Yönlendirme yoksa bu adım çalışır)
        Debug.Log("3. 🗣️ Yanıt TTS'e Gönderiliyor ve Oynatılıyor...");
        yield return StartCoroutine(SendTtsRequest(geminiReply));
        
        // Bellek temizliği
        Destroy(recordedClip);
    }
    
    // --- STT İsteği (Multipart/Form-Data) ---
    IEnumerator SendSTTRequest(AudioClip clip, System.Action<string> callback)
    {
        string url = BASE_URL + "/stt";

        // WAVUtility kullanarak AudioClip'i WAV byte dizisine çevir
        byte[] wavBytes = WavUtility.FromAudioClip(clip);
        
        // Multipart FormData oluşturma
        WWWForm form = new WWWForm();
        // İsim "file" olmalı, çünkü app.py "if 'file' not in request.files:" kontrolü yapıyor.
        form.AddBinaryData("file", wavBytes, "recording.wav", "audio/wav"); 
        
        UnityWebRequest request = UnityWebRequest.Post(url, form);
        request.timeout = 30; // Uzun bir ses kaydı için timeout'u uzat.

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"STT Hatası: {request.error}");
            callback("Konuşma algılanamadı.");
        }
        else
        {
            // Yanıt JSON'unu parse et (Örnek: {"text": "..."})
            try
            {
                string json = request.downloadHandler.text;
                STTResponse response = JsonUtility.FromJson<STTResponse>(json);
                callback(response.text.Trim());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"STT Yanıtı Okuma Hatası: {e.Message}");
                callback("Konuşma algılanamadı.");
            }
        }
    }

    // --- CHAT İsteği (JSON) ---
    IEnumerator SendChatRequest(string text, System.Action<string> callback)
    {
        string url = BASE_URL + "/chat";
        
        var requestData = new ChatRequestData 
        {
            text = text, 
            context = "" 
        };
        
        string json = JsonUtility.ToJson(requestData); 
        
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json; charset=utf-8"); // UTF-8 zorlandı

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Chat Hatası: {request.error}");
            callback(null);
        }
        else
        {
            try
            {
                string responseJson = request.downloadHandler.text;
                if (string.IsNullOrEmpty(responseJson))
                {
                    Debug.LogError("Server'dan boş yanıt gövdesi alındı.");
                    callback(null);
                    yield break;
                }

                ChatResponse response = JsonUtility.FromJson<ChatResponse>(responseJson);
                
                if (string.IsNullOrEmpty(response.reply))
                {
                    Debug.LogWarning("Gemini'den yanıt alındı, ancak içerik boştu (Filtrelenmiş olabilir).");
                    callback(null);
                }
                else
                {
                    callback(response.reply);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Chat yanıtı okunamadı. Hata: {e.Message}. Gelen metin: {request.downloadHandler.text.Substring(0, Mathf.Min(request.downloadHandler.text.Length, 100))}...");
                callback(null);
            }
        }
    }
    
    // --- TTS İsteği (Ses dosyası alma) ---
    IEnumerator SendTtsRequest(string text)
    {
        string url = BASE_URL + "/tts";
        
        var requestData = new TtsRequestData 
        {
            text = text 
        };
        string json = JsonUtility.ToJson(requestData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG); 
        request.SetRequestHeader("Content-Type", "application/json; charset=utf-8"); 

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"TTS Hatası: {request.error}. Server Yanıtı: {request.downloadHandler.text}");
        }
        else
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                Debug.Log("➡️ Ses dosyası oynatılıyor.");
                
                // <<< KRİTİK EKLENTİ: Oynatma bitene kadar bekle >>>
                // audioSource.isPlaying doğru olduğu sürece Coroutine'i duraklat
                while (audioSource.isPlaying)
                {
                    yield return null; // Bir sonraki frame'i bekle
                }
                // <<< EKLENTİ BİTTİ >>>
            }
        }
    }
}