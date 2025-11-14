# therapy_session.py
# App'in import ettiği minimal uyumlu implementasyonlar:
# - initialize_client
# - generate_therapist_response (Gemini)
# - synthesize_speech (AWS Polly)

import os
import boto3

def initialize_client(token_id: str):
    """
    Poe kullanılmıyor. Sadece sağlayıcı bilgisini taşıyan basit bir client döndürüyoruz.
    Gemini anahtarı ortam değişkeninden (GEMINI_API_KEY) okunacak.
    """
    return {"provider": "gemini", "token_id": token_id}


def generate_therapist_response(client, prompt_message: str, token_id: str, chat_id: str) -> str:
    """
    Google Gemini ile metin üretir.
    Gerekli:
      pip install google-generativeai
      export GEMINI_API_KEY="...."
    """
    import google.generativeai as genai

    api_key = os.getenv("GEMINI_API_KEY")
    if not api_key:
        raise RuntimeError("GEMINI_API_KEY ortam değişkeni ayarlı değil.")

    genai.configure(api_key=api_key)

    # Mümkün olan bir model adı: gemini-1.5-pro (yoksa flash'a düş)
    model_name = "gemini-1.5-pro"
    try:
        model = genai.GenerativeModel(model_name)
        resp = model.generate_content(prompt_message)
    except Exception:
        model = genai.GenerativeModel("gemini-1.5-flash")
        resp = model.generate_content(prompt_message)

    # SDK sürümleri arasında alan isimleri değişebildiği için güvenli çıkarım
    text = ""
    if hasattr(resp, "text") and resp.text:
        text = resp.text
    elif hasattr(resp, "candidates") and resp.candidates:
        for c in resp.candidates:
            if getattr(c, "content", None) and getattr(c.content, "parts", None):
                for p in c.content.parts:
                    if getattr(p, "text", None):
                        text += p.text

    text = (text or "").strip()
    if not text:
        text = "Seni duyuyorum. Birlikte ele alabiliriz — biraz daha detay paylaşır mısın?"
    return text


def synthesize_speech(access_key: str, secret_key: str, region: str,
                      voice_id: str, out_format: str, text: str, out_path: str):
    """
    AWS Polly ile TTS.
    Not: Bazı bölgelerde 'Arthur' olmayabilir. Hata alırsan 'Matthew' veya 'Joanna' dene.
    """
    session = boto3.Session(
        aws_access_key_id=access_key,
        aws_secret_access_key=secret_key,
        region_name=region
    )
    polly = session.client("polly")

    try:
        response = polly.synthesize_speech(
            Text=text,
            OutputFormat=out_format.upper(),
            VoiceId=voice_id
        )
    except polly.exceptions.InvalidParameterValueException:
        # Ses bulunamazsa Matthew’e düş
        response = polly.synthesize_speech(
            Text=text,
            OutputFormat=out_format.upper(),
            VoiceId="Matthew"
        )

    audio_stream = response.get("AudioStream")
    if not audio_stream:
        raise RuntimeError("Polly ses akışı alınamadı.")

    with open(out_path, "wb") as f:
        f.write(audio_stream.read())


