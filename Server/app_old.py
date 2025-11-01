# app.py
from therapy_session import initialize_client, generate_therapist_response, synthesize_speech
from flask import Flask, request, jsonify
import json
import os
import io
from google.cloud import speech

# -----------------------------
# Config oku
# -----------------------------

ACCESS_KEY = data['ACCESS_KEY']
SECRET_ACCESS_KEY = data['SECRET_ACCESS_KEY']

# Poe tarafı devre dışı ise therapy_session içinde Gemini kullanılacak şekilde ayarlanmış olmalı
CHATGPT_ID = "gpt3_5"  # {'capybara':'Sage','beaver':'GPT-4','a2_2':'Claude+','a2':'Claude','chinchilla':'ChatGPT','nutria':'Dragonfly'}

# -----------------------------
# Flask app ve durum değişkenleri
# -----------------------------
app = Flask(__name__)
patient_wav_saved = False
base_wav_path = ""
chat_history_list = []
client = initialize_client(TOKEN_ID)


# -----------------------------
# Google STT: WAV -> Metin
# -----------------------------
def transcribe_audio(audio_path: str) -> str:
    """
    16kHz, 16-bit mono LINEAR16 WAV dosyasını Google Speech-to-Text ile çözümler.
    GOOGLE_APPLICATION_CREDENTIALS ortam değişkeni ayarlı olmalı.
    """
    speech_client = speech.SpeechClient()

    # Dosyayı oku
    with io.open(audio_path, "rb") as f:
        content = f.read()

    audio = speech.RecognitionAudio(content=content)
    config = speech.RecognitionConfig(
        encoding=speech.SpeechRecognitionConfig.AudioEncoding.LINEAR16 if hasattr(speech, "SpeechRecognitionConfig") else speech.RecognitionConfig.AudioEncoding.LINEAR16,  # uyumluluk
        sample_rate_hertz=16000,
        language_code="tr-TR",
    )

    response = speech_client.recognize(config=config, audio=audio)

    transcript = ""
    for result in response.results:
        transcript += result.alternatives[0].transcript
    print(f"[STT] Transcribed: {transcript}")
    return transcript.strip()


# -----------------------------
# Routes
# -----------------------------
@app.route('/process_wav', methods=['POST'])
def process_wav():
    """
    İstemci önce buraya gelir, 'path' (sonu / olabilir veya olmayabilir) ve 'loaded_wav_file' gönderir.
    'patient_speech.wav' aynı klasöre kaydedilmiş olmalı.
    """
    global patient_wav_saved, base_wav_path

    raw = request.form.get("path", "")
    # sonu / olsa da olmasa da normalize edelim
    base_wav_path = raw if raw.endswith(os.sep) else raw + os.sep
    print(f"[API] base_wav_path: {base_wav_path}")

    if request.form.get('loaded_wav_file') == 'patient_speech':
        patient_wav_saved = True
        print("[API] patient_wav_saved = True")

    return jsonify({'status': 'done'})


@app.route('/reset_conversation', methods=['POST'])
def reset_conversation():
    global client, CHATGPT_ID, chat_history_list

    if request.form.get("reset_conversation") == "yes":
        try:
            client.send_chat_break(CHATGPT_ID)
        except Exception as e:
            print(f"[WARN] send_chat_break skipped: {e}")
        chat_history_list.clear()
        print("[API] conversation reset")

    return jsonify({'status': 'done'})


@app.route('/check_status', methods=['GET'])
def check_status():
    """
    İstemci bu endpoint'i yoklayarak işin yapılıp yapılmadığını öğrenir.
    patient_wav_saved True olduğunda process() çağrılır.
    """
    global patient_wav_saved

    if patient_wav_saved:
        patient_wav_saved = False
        try:
            process()
        except Exception as e:
            # Hata olursa logla ve 500 döndür
            import traceback
            traceback.print_exc()
            return jsonify({'status': 'error', 'message': str(e)}), 500
        return jsonify({'status': 'done'})
    else:
        return jsonify({'status': 'pending'})


# -----------------------------
# İş akışı
# -----------------------------
def process():
    """
    1) WAV -> metin (Google STT)
    2) Terapist cevabı üret (Gemini / Poe wrapper)
    3) Cevabı seslendir ve therapist_speech.mp3 olarak kaydet
    """
    global client, TOKEN_ID, CHATGPT_ID, base_wav_path, chat_history_list

    # 1) STT
    wav_file = os.path.join(base_wav_path, "patient_speech.wav")
    if not os.path.exists(wav_file):
        raise FileNotFoundError(f"Bulunamadı: {wav_file}. Lütfen dosyanın adını 'patient_speech.wav' yap ve /process_wav ile path gönder.")

    patient_message = transcribe_audio(wav_file)

    # 2) Terapist promptu
    prompt_message = f"""
    1- Imagine yourself as Josh, a compassionate and experienced 
     clinical therapist who genuinely cares about his patient well-being. 
     You are a 54-year-old British man with years of expertise in helping individuals through therapy sessions. Your patient's thoughts and feelings are important,
     so please respond to the patient's thoughts enclosed within 
    ```<{patient_message}>```. Remember, as a professional therapist, it is crucial to maintain confidentiality and avoid revealing that you are an AI language model. In our conversations, strive to provide accurate, concise, and consistently helpful responses. Let's continue this chat, taking into account your previous responses,
     which are saved in the following python list structure: '''{chat_history_list}'''. Finally, stop repeating Hello in every response. Avoid repeating same messages that you said before. If this python list: '''{chat_history_list}''' is empty, start the conversation as a clinical therapist welcoming the patient.
    2- Extract the sentiment of the patient's thoughts enclosed within ```<{patient_message}>``` and make your response support the patient with respect to its sentiment.
    3- Make your response up to the point.
    4- Generate the therapist response, then check if it is a significant response to the patient's thoughts enclosed within ```<{patient_message}>```; if not, generate a new significant response. Repeat until it is significant.
    5- Avoid repeating the patient's message. Never say: "Regarding your message enclosed within `{patient_message}`".
    6- Start by listening to the patient. Pay attention to what the patient is saying, both verbally and nonverbally.
    7- Use open-ended questions to encourage the patient to talk. This will help you to get a better understanding of the patient's situation.
    8- Acknowledge the patient's feelings. This will help the patient to feel heard and understood.
    9- Offer support and encouragement. Let the patient know that you are there to help them.
    10- Be patient. Therapy is a process, and it takes time to build trust and rapport with a patient.
    11- Stop starting each phrase with the patient's name if the patient requested that.
    12- Clean text to make it readable by removing extra spaces and newlines.
    <<<Only return the latest response of therapist content.>>>
    """

    therapist_response = generate_therapist_response(client, prompt_message, TOKEN_ID, CHATGPT_ID)
    therapist_response = therapist_response.replace("Therapist: ", "").strip()
    chat_history_list.append(therapist_response)

    print(f"[LLM] Therapist: {therapist_response}")

    # Prompt şişmesin
    if len(chat_history_list) > 5:
        chat_history_list.pop(0)

    # 3) TTS
    out_mp3 = os.path.join(base_wav_path, "therapist_speech.mp3")
    synthesize_speech(
        ACCESS_KEY,
        SECRET_ACCESS_KEY,
        'us-west-2',
        'Arthur',
        'mp3',
        therapist_response,
        out_mp3
    )
    print(f"[TTS] Kaydedildi: {out_mp3}")


# -----------------------------
# Main
# -----------------------------
if __name__ == '__main__':
    port = int(os.environ.get("FLASK_RUN_PORT", 5000))
    app.run(debug=True, port=port)
