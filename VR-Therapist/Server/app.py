# app.py (temiz, Gemini + Google STT, dummy TTS)
from flask import Flask, request, jsonify, send_file
import io, json
import google.generativeai as genai
#from google.cloud import speech_v1p1beta1 as speech
#from google.cloud import texttospeech_v1 as texttospeech  # <-- değişiklik




# --- config ---
with open("config.json", "r", encoding="utf-8") as f:
    CFG = json.load(f)

GEMINI_API_KEY = CFG.get("GEMINI_API_KEY", "")
if not GEMINI_API_KEY:
    raise RuntimeError("GEMINI_API_KEY missing in config.json")

genai.configure(api_key=GEMINI_API_KEY)

app = Flask(__name__)

# --- health ---
@app.get("/health")
def health():
    return jsonify({"ok": True}), 200

# --- chat (Gemini) ---
@app.post("/chat")
def chat():
    data = request.get_json(force=True, silent=True) or {}
    user_text = (data.get("text") or "").strip()
    context = (data.get("context") or "").strip()
    if not user_text:
        return jsonify({"error": "text is required"}), 400

    system_prompt = (
        "You are a supportive, CBT-informed assistant. "
        "Be empathetic, concise, and offer concrete next steps. "
        "Avoid medical diagnosis; encourage seeking professional help in crisis."
    )
    prompt = f"{system_prompt}\n\nContext: {context}\n\nUser: {user_text}"

    model = genai.GenerativeModel("gemini-2.0-flash")
    resp = model.generate_content(prompt)
    reply = getattr(resp, "text", "").strip()
    return jsonify({"reply": reply})

# --- stt (Google) ---
@app.post("/stt")
def stt():
    if "file" not in request.files:
        return jsonify({"error": "file is required"}), 400
    audio_bytes = request.files["file"].read()

    client = speech.SpeechClient()
    audio = speech.RecognitionAudio(content=audio_bytes)
    config = speech.RecognitionConfig(
        # emin değilsen ENCODING_UNSPECIFIED güvenli
        encoding=speech.RecognitionConfig.AudioEncoding.ENCODING_UNSPECIFIED,
        language_code="tr-TR",
        enable_automatic_punctuation=True,
    )
    response = client.recognize(config=config, audio=audio)
    text = " ".join([r.alternatives[0].transcript for r in response.results]) if response.results else ""
    return jsonify({"text": text})

# --- tts (şimdilik dummy metin) ---
@app.post("/tts")
def tts():
    data = request.get_json(force=True, silent=True) or {}
    text = (data.get("text") or "").strip()
    if not text:
        return jsonify({"error": "text is required"}), 400

    try:
        client = texttospeech.TextToSpeechClient()
        synthesis_input = texttospeech.SynthesisInput(text=text)
        voice = texttospeech.VoiceSelectionParams(
            language_code="tr-TR",
            ssml_gender=texttospeech.SsmlVoiceGender.NEUTRAL,
        )
        audio_config = texttospeech.AudioConfig(
            audio_encoding=texttospeech.AudioEncoding.MP3,
            speaking_rate=1.0
        )
        resp = client.synthesize_speech(
            input=synthesis_input, voice=voice, audio_config=audio_config
        )
        return send_file(
            io.BytesIO(resp.audio_content),
            mimetype="audio/mpeg",
            as_attachment=False,
            download_name="speech.mp3",
        )
    except Exception as e:
        import traceback; traceback.print_exc()
        return jsonify({"error": str(e)}), 500

if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5001, debug=True)
