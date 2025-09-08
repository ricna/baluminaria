using UnityEngine;
using MidiPlayerTK;

public class BaluAudioReactive : MonoBehaviour
{
    [SerializeField] private BaluMidiController _baluMidiController;

    [Header("Configurações do Áudio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private float _minVolumeThreshold = 0.1f;
    [SerializeField] private int _midiNoteBase = 60; // Nota MIDI para volume mínimo

    // NOVO: Controle de frequência da análise
    [SerializeField]
    [Tooltip("Intervalo em segundos entre cada análise de espectro.")]
    private float _analysisInterval = 0.05f;
    private float _analysisTimer = 0f;

    private float[] _spectrumData = new float[128];

    private void Update()
    {
        // NOVO: Lógica do temporizador
        _analysisTimer += Time.deltaTime;
        if (_analysisTimer < _analysisInterval)
        {
            return; // Pula a análise neste frame se o intervalo não foi atingido
        }
        _analysisTimer = 0f; // Reseta o timer

        // O resto do código é o mesmo, mas agora só executa periodicamente
        if (_audioSource == null || _baluMidiController == null)
        {
            return;
        }

        _audioSource.GetSpectrumData(_spectrumData, 0, FFTWindow.Rectangular);

        float maxVolume = 0;
        int maxVolumeIndex = -1;

        for (int i = 0; i < _spectrumData.Length; i++)
        {
            if (_spectrumData[i] > maxVolume)
            {
                maxVolume = _spectrumData[i];
                maxVolumeIndex = i;
            }
        }

        if (maxVolume > _minVolumeThreshold && maxVolumeIndex != -1)
        {
            int midiNote = _midiNoteBase + maxVolumeIndex;
            int velocity = Mathf.RoundToInt(Mathf.Clamp(maxVolume * 127f, 0, 127));

            _baluMidiController.HandleNoteOn(midiNote, velocity);
        }
    }
}