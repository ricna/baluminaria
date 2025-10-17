using UnityEngine;
using MidiPlayerTK;
using System.Linq;
using System;

public class BaluAudioReactive : MonoBehaviour
{
    [SerializeField] private BaluMidiController _baluMidiController;

    public enum AudioInputMode
    {
        AudioSource,
        Microphone
    }

    [Header("Configura��es do �udio")]
    [SerializeField] private AudioInputMode _audioInputMode = AudioInputMode.AudioSource;
    [SerializeField] private AudioSource _audioSource;

    [Header("Debug")]
    [SerializeField] private float _noteOnThreshold = 0.05f; // Limiar para considerar uma nota "ligada"
    [SerializeField] private float _noteOffThreshold = 0.01f; // Limiar para considerar uma nota "desligada"
    private float _analysisTimer = 0f;

    // Configura��es para an�lise de frequ�ncia
    [SerializeField] private int _numberOfSamples = 1024; // Deve ser pot�ncia de 2 (64, 128, 256, 512, 1024, 2048, etc.)
    [SerializeField] private float _minFrequency = 20f; // Frequ�ncia m�nima a ser considerada (Hz)
    [SerializeField] private float _maxFrequency = 10000f; // Frequ�ncia m�xima a ser considerada (Hz)

    private float[] _spectrumData;
    private float[] _audioBuffer; // Buffer para dados de �udio do microfone
    private int _sampleRate; // Taxa de amostragem do �udio

    private const float A4_FREQUENCY = 440f;
    private const int A4_MIDI_NOTE = 69;

    private bool[] _activeMidiNotes = new bool[128]; // Para controlar o estado das notas MIDI

    private void Awake()
    {
        _spectrumData = new float[_numberOfSamples];
        _audioBuffer = new float[_numberOfSamples];
        _sampleRate = AudioSettings.outputSampleRate;
    }

    private void Start()
    {
        if (_audioInputMode == AudioInputMode.Microphone)
        {
            StartMicrophone();
        }

    }

    private void OnDisable()
    {
        if (_audioInputMode == AudioInputMode.Microphone && Microphone.IsRecording(_audioSource.clip.name))
        {
            Microphone.End(_audioSource.clip.name);
        }
    }

    private void StartMicrophone()
    {
        if (_audioSource == null) return;

        // Verifica se j� est� gravando para evitar iniciar m�ltiplas vezes
        if (Microphone.IsRecording(null))
        {
            Debug.LogWarning("Microfone j� est� gravando. Parando grava��o existente.");
            Microphone.End(null);
        }

        // Inicia a grava��o do microfone no AudioSource
        _audioSource.clip = Microphone.Start(null, true, 1, _sampleRate);
        _audioSource.loop = true; // Loop para grava��o cont�nua
        while (!(Microphone.GetPosition(null) > 0)) { } // Espera o microfone come�ar a gravar
        _audioSource.Play();
        Debug.Log("Microfone iniciado com sucesso.");
    }

    private void Update()
    {
        _analysisTimer += Time.deltaTime;
        if (_analysisTimer < _baluMidiController.GetInterval())
        {
            return;
        }
        _analysisTimer = 0f;

        if (_baluMidiController == null) return;

        if (_audioInputMode == AudioInputMode.Microphone)
        {
            if (_audioSource == null || !_audioSource.isPlaying) return;
            // Pega os dados do microfone
            _audioSource.GetOutputData(_audioBuffer, 0);
            _audioSource.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris); // Usar BlackmanHarris para melhor precis�o de frequ�ncia
        }
        else // AudioSource (File/Stream)
        {
            if (_audioSource == null || !_audioSource.isPlaying) return;
            _audioSource.GetSpectrumData(_spectrumData, 0, FFTWindow.BlackmanHarris);
        }

        ProcessSpectrumData();
    }

    private void ProcessSpectrumData()
    {
        float maxFreq = _sampleRate / 2f; // Nyquist frequency
        float binWidth = maxFreq / _numberOfSamples;

        for (int i = 0; i < _numberOfSamples; i++)
        {
            float freq = i * binWidth;

            // Ignora frequ�ncias fora do range desejado
            if (freq < _minFrequency || freq > _maxFrequency)
            {
                continue;
            }

            float intensity = _spectrumData[i];

            // Converte frequ�ncia para nota MIDI
            int midiNote = FrequencyToMidiNote(freq);

            if (midiNote >= 0 && midiNote < 128)
            {
                _noteOnThreshold = _baluMidiController.GetSensitivity();
                _noteOffThreshold = 10;// Mathf.Clamp(_noteOffThreshold, _noteOnThreshold, _noteOnThreshold * 2);
                if (intensity > _noteOnThreshold && !_activeMidiNotes[midiNote])
                {
                    // Nota ligada
                    int velocity = Mathf.RoundToInt(Mathf.Clamp01(intensity / _noteOnThreshold) * 127f);
                    _baluMidiController.HandleNoteOn(midiNote, velocity);
                    _activeMidiNotes[midiNote] = true;
                }
                else if (intensity < _noteOffThreshold && _activeMidiNotes[midiNote])
                {
                    // Nota desligada
                    _baluMidiController.HandleNoteOn(midiNote, 0);
                    _activeMidiNotes[midiNote] = false;
                }
            }
        }
    }

    private int FrequencyToMidiNote(float frequency)
    {
        if (frequency <= 0) return -1;
        return Mathf.RoundToInt(A4_MIDI_NOTE + 12 * Mathf.Log(frequency / A4_FREQUENCY, 2));
    }

    // M�todos para alternar o modo de entrada de �udio em tempo de execu��o (opcional)
    public void SetAudioInputMode(AudioInputMode mode)
    {
        if (_audioInputMode == mode) return;

        // Para a grava��o atual, se houver
        if (_audioInputMode == AudioInputMode.Microphone && Microphone.IsRecording(null))
        {
            Microphone.End(null);
            _audioSource.Stop();
        }

        _audioInputMode = mode;

        if (_audioInputMode == AudioInputMode.Microphone)
        {
            StartMicrophone();
        }
        else // AudioSource
        {
            // Certifique-se de que o AudioSource est� configurado para reproduzir um clipe se necess�rio
            // e n�o est� tentando gravar do microfone.
            if (_audioSource.clip != null && !_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }
    }

    public void RestartAudioClip()
    {
        if (_audioInputMode == AudioInputMode.AudioSource && _audioSource != null && _audioSource.clip != null)
        {
            _audioSource.Stop();
            _audioSource.Play();
        }
    }
}


