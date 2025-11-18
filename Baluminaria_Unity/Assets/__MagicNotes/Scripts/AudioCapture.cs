using System;
using System.IO;
using System.Threading;
using UnityEngine;

public class AudioCapture : MonoBehaviour
{
    public static AudioCapture Instance;

    private const int SampleRate = 44100;
    private const int Channels = 2;

    private MemoryStream audioBuffer;
    private bool capturingAudio = false;
    private object bufferLock = new object();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Chamado pelo VideoRecorder
    public void StartRecording()
    {
        if (capturingAudio)
        {
            return;
        }

        capturingAudio = true;
        audioBuffer = new MemoryStream(10 * 1024 * 1024); // 10 MB inicial

        UnityEngine.Debug.Log("AudioCapture: recording started.");
    }

    public void StopRecording()
    {
        capturingAudio = false;
        UnityEngine.Debug.Log("AudioCapture: recording stopped.");
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!capturingAudio)
        {
            return;
        }

        int length = data.Length;

        byte[] bytes = new byte[length * sizeof(short)];

        for (int i = 0; i < length; i++)
        {
            short sample = (short)(Mathf.Clamp(data[i], -1f, 1f) * short.MaxValue);
            int index = i * 2;
            bytes[index] = (byte)(sample & 0xFF);
            bytes[index + 1] = (byte)((sample >> 8) & 0xFF);
        }

        lock (bufferLock)
        {
            audioBuffer.Write(bytes, 0, bytes.Length);
        }
    }

    public void SaveWav(string filePath)
    {
        if (audioBuffer == null)
        {
            UnityEngine.Debug.LogError("AudioCapture: No audio buffer to save!");
            return;
        }

        byte[] audioData;

        lock (bufferLock)
        {
            audioData = audioBuffer.ToArray();
        }

        FileStream fileStream = new FileStream(filePath, FileMode.Create);
        BinaryWriter writer = new BinaryWriter(fileStream);

        int headerSize = 44;
        int dataSize = audioData.Length;
        int fileSize = headerSize + dataSize - 8;

        // RIFF header
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(fileSize);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

        // fmt chunk
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)Channels);
        writer.Write(SampleRate);
        writer.Write(SampleRate * Channels * 2);
        writer.Write((short)(Channels * 2));
        writer.Write((short)16);

        // data chunk
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);
        writer.Write(audioData);

        writer.Close();
        fileStream.Close();

        UnityEngine.Debug.Log("AudioCapture: WAV saved: " + filePath);
    }
}
