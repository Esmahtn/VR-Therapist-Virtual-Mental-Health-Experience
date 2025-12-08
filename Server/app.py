# app.py  – Gemini HTTP API + Google STT + Google TTS

from flask import Flask, request, jsonify, send_file
import io
import json
import requests

from google.cloud import speech_v1p1beta1 as speech
from google.cloud import texttospeech_v1 as texttospeech

# -------------------------------------------------
# config.json'dan API key oku
# -------------------------------------------------
with open("config.json", "r", encoding="utf-8") as f:
    CFG = json.load(f)

GEMINI_API_KEY = CFG.get("GEMINI_API_KEY", "")
if not GEMINI_API_KEY:
    raise RuntimeError("GEMINI_API_KEY missing in config.json")

# HTTP endpoint (Google Cloud projen için)
GEMINI_ENDPOINT = (
    "https://generativelanguage.googleapis.com/v1beta/"
    "models/gemini-2.0-flash:generateContent"
)

app = Flask(__name__)

# -------------------------------------------------
# health
# -------------------------------------------------
@app.get("/health")
def health():
    return jsonify({"ok": True}), 200


# -------------------------------------------------
# chat (Gemini HTTP)
# -------------------------------------------------
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

    # HTTP body – v1beta generateContent formatı
    body = {
        "contents": [
            {
                "role": "user",
                "parts": [{"text": prompt}],
            }
        ]
    }

    try:
        resp = requests.post(
            GEMINI_ENDPOINT,
            params={"key": GEMINI_API_KEY},
            json=body,
            timeout=30,
        )

        # Hata kodu varsa logla ve fallback dön
        if resp.status_code != 200:
            print("=== GEMINI HTTP ERROR ===")
            print("Status:", resp.status_code)
            try:
                print("Response JSON:", resp.json())
            except Exception:
                print("Raw response:", resp.text)
            print("=== GEMINI HTTP ERROR END ===")

            return jsonify(
                {
                    "reply": (
                        "Şu anda konuşma modeliyle bağlantı kurulamadı "
                        f"(HTTP {resp.status_code}). "
                        "Lütfen biraz sonra tekrar dener misin?"
                    )
                }
            )

        data = resp.json()

        # Yanıttan metni çek
        candidates = data.get("candidates") or []
        if not candidates:
            reply = "Şu anda sana yanıt üretirken bir sorun oluştu."
        else:
            # İlk candidate, ilk part, text
            parts = candidates[0].get("content", {}).get("parts") or []
            reply = ""
            for p in parts:
                if "text" in p:
                    reply += p["text"]
            reply = reply.strip() or "Şu anda sana yanıt üretirken bir sorun oluştu."

    except Exception as e:
        import traceback

        print("=== GEMINI EXCEPTION ===")
        traceback.print_exc()
        print("=== GEMINI EXCEPTION END ===")

        reply = (
            "Şu anda konuşma modeliyle bağlantı kurulamadı. "
            "Lütfen biraz sonra tekrar dener misin?"
        )

    return jsonify({"reply": reply})


# -------------------------------------------------
# stt (Google)
# -------------------------------------------------
@app.post("/stt")
def stt():
    if "file" not in request.files:
        return jsonify({"error": "file is required"}), 400

    audio_bytes = request.files["file"].read()

    client = speech.SpeechClient()
    audio = speech.RecognitionAudio(content=audio_bytes)
    config = speech.RecognitionConfig(
        encoding=speech.RecognitionConfig.AudioEncoding.ENCODING_UNSPECIFIED,
        language_code="tr-TR",
        enable_automatic_punctuation=True,
    )

    response = client.recognize(config=config, audio=audio)
    text = (
        " ".join(r.alternatives[0].transcript for r in response.results)
        if response.results
        else ""
    )

    return jsonify({"text": text})


# -------------------------------------------------
# tts (Google)
# -------------------------------------------------
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
            speaking_rate=1.0,
        )

        resp = client.synthesize_speech(
            input=synthesis_input,
            voice=voice,
            audio_config=audio_config,
        )

        return send_file(
            io.BytesIO(resp.audio_content),
            mimetype="audio/mpeg",
            as_attachment=False,
            download_name="speech.mp3",
        )

    except Exception as e:
        import traceback

        traceback.print_exc()
        return jsonify({"error": str(e)}), 500


# -------------------------------------------------
if __name__ == "__main__":
    app.run(host="0.0.0.0", port=5001, debug=True)
