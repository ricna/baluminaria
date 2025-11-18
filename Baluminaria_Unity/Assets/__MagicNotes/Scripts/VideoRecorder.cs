using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class VideoRecorder : MonoBehaviour
{
    public Camera recordCamera;
    public RenderTexture targetRenderTexture;
    public int captureFramerate = 30;
    public string tempFolder = "RecordTemp";
    public string ffmpegPath = "ffmpeg.exe";

    public bool useJpg = false; // PNG = qualidade melhor; JPG = muito mais leve

    private bool isRecording = false;
    private int frameIndex = 0;
    private string audioPath;
    private object writeLock = new object();

    private void Start()
    {
        if (recordCamera != null && targetRenderTexture == null)
        {
            targetRenderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            recordCamera.targetTexture = targetRenderTexture;
        }
    }

    public void StartRecording()
    {
        if (isRecording)
        {
            return;
        }

        string folder = Path.Combine(Application.persistentDataPath, tempFolder);
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        frameIndex = 0;

        isRecording = true;
        Time.captureFramerate = captureFramerate;

        audioPath = Path.Combine(folder, "audio.wav");
        AudioCapture.Instance.StartRecording();

        UnityEngine.Debug.Log("VideoRecorder: Recording started.");
    }

    public void StopRecording(string outputFileName)
    {
        if (!isRecording)
        {
            return;
        }

        isRecording = false;
        Time.captureFramerate = 0;

        AudioCapture.Instance.StopRecording();
        AudioCapture.Instance.SaveWav(audioPath);

        string outputPath = Path.Combine(Application.persistentDataPath, outputFileName);

        Thread encodeThread = new Thread(() => EncodeWithFFmpeg(outputPath));
        encodeThread.Start();

        UnityEngine.Debug.Log("VideoRecorder: Stopped. Encoding...");
    }

    private void Update()
    {
        if (isRecording)
        {
            CaptureFrame();
        }
    }

    private void CaptureFrame()
    {
        AsyncGPUReadback.Request(targetRenderTexture, 0, TextureFormat.RGBA32, readback =>
        {
            if (readback.hasError)
            {
                UnityEngine.Debug.LogError("VideoRecorder: GPU readback error.");
                return;
            }

            NativeArray<byte> raw = readback.GetData<byte>();
            int width = targetRenderTexture.width;
            int height = targetRenderTexture.height;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.LoadRawTextureData(raw);
            tex.Apply();

            string extension = useJpg ? ".jpg" : ".png";
            string fileName = "frame_" + frameIndex.ToString("D6") + extension;
            string fullPath = Path.Combine(Application.persistentDataPath, tempFolder, fileName);

            ThreadPool.QueueUserWorkItem(state =>
            {
                byte[] bytes;

                if (useJpg)
                {
                    bytes = tex.EncodeToJPG(90); // compressão boa
                }
                else
                {
                    bytes = tex.EncodeToPNG();
                }

                lock (writeLock)
                {
                    File.WriteAllBytes(fullPath, bytes);
                }

                UnityEngine.Object.Destroy(tex);
            });

            frameIndex++;
        });
    }

    private void EncodeWithFFmpeg(string outputPath)
    {
        string folder = Path.Combine(Application.persistentDataPath, tempFolder);
        string pattern = useJpg ? "frame_%06d.jpg" : "frame_%06d.png";

        string args =
            "-framerate " + captureFramerate +
            " -i \"" + Path.Combine(folder, pattern) + "\"" +
            " -i \"" + audioPath + "\"" +
            " -c:v libx264 -pix_fmt yuv420p -crf 18" +
            " -c:a aac -b:a 192k" +
            " \"" + outputPath + "\"";

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = ffmpegPath;
        psi.Arguments = args;
        psi.UseShellExecute = false;
        psi.RedirectStandardError = true;
        psi.RedirectStandardOutput = true;
        psi.CreateNoWindow = true;

        using (Process proc = Process.Start(psi))
        {
            proc.WaitForExit();
            string log = proc.StandardError.ReadToEnd();
            UnityEngine.Debug.Log("FFmpeg LOG:\n" + log);
        }

        UnityEngine.Debug.Log("VideoRecorder: Encoding complete: " + outputPath);
    }
}
