using UnityEngine;
using System;
using System.IO;

public static class WavUtility
{
    // AudioClip'i WAV formatında byte dizisine dönüştürür.
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            // Veri alma ve başlık ekleme sırası önemlidir.
            
            // 1. Ses verilerini float dizisinden byte dizisine çevirme
            // PCM (Pulse Code Modulation) verisini alıyoruz.
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            // Her float (tek hassasiyetli) 4 byte'tır. Google STT genellikle 16-bit
            // küçük endian (Little Endian) integer ister.
            short[] intSamples = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                // Float (-1.0f ile 1.0f arası) değerini 16-bit'e (±32767) ölçeklendir
                intSamples[i] = (short)(samples[i] * short.MaxValue);
            }

            byte[] byteArray = new byte[intSamples.Length * 2]; // 16 bit = 2 byte
            Buffer.BlockCopy(intSamples, 0, byteArray, 0, byteArray.Length);

            // 2. WAV Başlıkları (Header) Yazma
            // WAV formatı Riff/Wave başlığı ile başlar.
            int riffSize = 36 + byteArray.Length;
            int totalSamples = samples.Length;
            int sampleRate = clip.frequency; // Genellikle 16000 veya 44100
            int channels = clip.channels;
            int byteRate = sampleRate * channels * 2; // 16 bit = 2 byte

            // ---- RIFF Chunk (Büyük Ana Blok) ----
            stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, 4); // Chunk ID: 'RIFF'
            stream.Write(BitConverter.GetBytes(riffSize), 0, 4);            // Chunk Size
            stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, 4); // Format: 'WAVE'

            // ---- FMT Sub-chunk (Format Bilgisi) ----
            stream.Write(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, 4); // Sub-chunk ID: 'fmt '
            stream.Write(BitConverter.GetBytes(16), 0, 4);                  // Sub-chunk size: 16 (PCM için)
            stream.Write(BitConverter.GetBytes((short)1), 0, 2);            // Audio Format: 1 (PCM)
            stream.Write(BitConverter.GetBytes((short)channels), 0, 2);     // Channel Count
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);          // Sample Rate
            stream.Write(BitConverter.GetBytes(byteRate), 0, 4);            // Byte Rate
            stream.Write(BitConverter.GetBytes((short)(channels * 2)), 0, 2);// Block Align
            stream.Write(BitConverter.GetBytes((short)16), 0, 2);           // Bits Per Sample: 16 bit

            // ---- DATA Sub-chunk (Ses Verisi) ----
            stream.Write(System.Text.Encoding.ASCII.GetBytes("data"), 0, 4); // Sub-chunk ID: 'data'
            stream.Write(BitConverter.GetBytes(byteArray.Length), 0, 4);     // Sub-chunk Size (Veri uzunluğu)
            
            // Son olarak ham ses verisini yaz
            stream.Write(byteArray, 0, byteArray.Length);

            return stream.ToArray();
        }
    }
}