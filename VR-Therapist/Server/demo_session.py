import os, sys, time, json, tempfile, subprocess
import requests
import sounddevice as sd
from scipy.io.wavfile import write as wav_write

import os
BASE = os.getenv("BACKEND_URL", "http://127.0.0.1:5001")



SR = 16000                      # 16 kHz mono önerilen
CH = 1

def record_wav(seconds=6, path="user.wav"):
    print(f"\n🎙️  {seconds} sn kayıt başlıyor... Konuş, bitince otomatik duracak.")
    audio = sd.rec(int(seconds*SR), samplerate=SR, channels=CH, dtype='int16')
    sd.wait()
    wav_write(path, SR, audio)
    print(f"💾 Kaydedildi: {path}")
    return path

def stt(wav_path):
    with open(wav_path, "rb") as f:
        resp = requests.post(f"{BASE}/stt", files={"file": f})
    try:
        data = resp.json()
    except Exception:
        raise RuntimeError(f"/stt beklenmeyen yanıt: {resp.status_code}")
    if resp.status_code != 200:
        raise RuntimeError(f"/stt hata: {data}")
    # Sunucunda 'transcript' veya 'text' olabilir; ikisini de dene:
    text = data.get("transcript") or data.get("text")
    if not text:
        raise RuntimeError(f"/stt metin yok: {data}")
    return text

def chat(user_text):
    resp = requests.post(f"{BASE}/chat", json={"text": user_text})
    data = resp.json()
    if resp.status_code != 200:
        raise RuntimeError(f"/chat hata: {data}")
    # Sunucunda 'reply' veya 'response' olabilir:
    return data.get("reply") or data.get("response") or json.dumps(data, ensure_ascii=False)

def tts(text, out_mp3="reply.mp3"):
    resp = requests.post(f"{BASE}/tts", json={"text": text})
    if resp.status_code != 200:
        try:
            print("⚠️ /tts hata:", resp.json())
        except Exception:
            print("⚠️ /tts hata, status:", resp.status_code)
        raise SystemExit(1)
    with open(out_mp3, "wb") as f:
        f.write(resp.content)
    return out_mp3

def play_mp3(path):
    # macOS: afplay
    try:
        subprocess.run(["afplay", path], check=True)
    except Exception as e:
        print(f"⚠️ Ses çalınamadı ({e}). mp3 dosyası: {path}")

def main():
    print("🧪 Mini Terapi Demo — Mic → STT → Chat → TTS → Ses")
    print("Komutlar: [Enter]=Yeni kayıt, 'q' + Enter = çıkış")
    while True:
        cmd = input("\n↩️ Enter: konuşmaya başla (6 sn), 'q': çık: ").strip().lower()
        if cmd == "q":
            break
        wav = record_wav(6, "user.wav")
        try:
            user_text = stt(wav)
            print(f"📝 STT: {user_text}")
        except Exception as e:
            print("❌ STT hata:", e); continue

        try:
            reply = chat(user_text)
            print(f"🤖 Cevap: {reply}")
        except Exception as e:
            print("❌ Chat hata:", e); continue

        try:
            mp3 = tts(reply, "reply.mp3")
            print(f"🔊 Ses dosyası: {mp3}")
            play_mp3(mp3)
        except SystemExit:
            pass
        except Exception as e:
            print("❌ TTS hata:", e)

    print("👋 Bitti.")

if __name__ == "__main__":
    main()
