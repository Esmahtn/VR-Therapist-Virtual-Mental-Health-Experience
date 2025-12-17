using System;
using System.IO;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        if (clip == null) return null;

        using (var stream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(stream))
            {
                int hz = clip.frequency;
                int channels = clip.channels;
                int samples = clip.samples;

                // RIFF header
                writer.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
                writer.Write(36 + samples * channels * 2);
                writer.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));

                // fmt chunk
                writer.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write((short)channels);
                writer.Write(hz);
                writer.Write(hz * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);

                // data chunk
                writer.Write(System.Text.Encoding.UTF8.GetBytes("data"));
                writer.Write(samples * channels * 2);

                // Actual audio data
                float[] floatData = new float[samples * channels];
                clip.GetData(floatData, 0);

                foreach (float f in floatData)
                {
                    writer.Write((short)(f * 32767f));
                }
            }
            return stream.ToArray();
        }
    }
}