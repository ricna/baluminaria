using UnityEngine;
using MidiPlayerTK;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BaluMIDI : MonoBehaviour
{
    
    // Enum para controlar o modo de reprodução
    public enum PlaybackMode
    {
        File,
        Stream,
        AudioSource
    }

    // Enum para controlar o modo de cor
    public enum ColorMode
    {
        ByRing,
        ByNote,
        FullRing, // O anel inteiro acende com a cor da nota.
        RandomNoteColor,
        FullBoardPulse,
        Strobe,
        AcrossOctaves, // Estende a cor do anel/oitava para todas as notas iguais.
        VerticalColor, // Estende a cor da nota cromática para todas as notas iguais.
        Cross // Combina o modo FullRing e o modo AcrossOctaves.
    }

    [Header("Componentes MPTK")]
    [SerializeField] private MidiFilePlayer _midiFilePlayer;
    [SerializeField] private MidiStreamPlayer _midiStreamPlayer;
    [SerializeField] private BaluAudioReactive _baluAudioReactive;
    [SerializeField] private AudioSource _audioSource;

    [Header("Exibição de Informações")]
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Transform _ringsParent;

    [Header("Configurações de Reprodução")]
    [Tooltip("Selecione o modo de reprodução: File para arquivos .mid, Stream para entrada MIDI ao vivo, AudioSource para arquivos de áudio.")]
    [SerializeField] private PlaybackMode _currentPlaybackMode = PlaybackMode.File;
    [Tooltip("Ativa ou desativa o comportamento de 'clamping' para notas fora do alcance (A0, A#0, B0, C8).")]
    [SerializeField] private bool _clampNotes = true;

    [Header("Modo de Cores")]
    [Tooltip("ByRing: Cor da oitava. ByNote: Cor da nota. FullRing: O anel inteiro acende com a cor da nota. AcrossOctaves: Cor do anel se espalha para notas iguais. VerticalColor: Cor da nota se espalha para notas iguais. Cross: Combina FullRing e AcrossOctaves.")]
    [SerializeField] private ColorMode _currentColorMode = ColorMode.ByRing;

    [Header("Cores por Ring (Arco-íris)")]
    [SerializeField]
    public Color[] _ringColors = new Color[7]
    {
        new Color(1f, 0f, 0f),       // Vermelho
        new Color(1f, 0.5f, 0f),     // Laranja
        new Color(1f, 1f, 0f),       // Amarelo
        new Color(0f, 1f, 0f),       // Verde       
        new Color(0f, 0f, 1f),       // Azul
        new Color(0.29f, 0f, 0.51f), // Anil (índigo)
        new Color(0.56f, 0f, 1f)     // Violeta
    };

    [Header("Cores por Nota (Cromático)")]
    [SerializeField]
    public Color[] _noteColors = new Color[12];

    [SerializeField] private Sprite _segSprite;

    [Header("Configurações Visuais")]
    [SerializeField] private float _dimIntensity = 0.1f;
    [SerializeField] private float _pulseDuration = 0.1f;
    [SerializeField] private float _strobeDuration = 0.2f;

    [Header("Atrasos")]
    [Tooltip("Atraso de fade-out para cada anel (oitava).")]
    [SerializeField] private float[] _ringDelays = new float[7];
    [Tooltip("Atraso de fade-out para cada nota cromática.")]
    [SerializeField] private float[] _noteDelays = new float[12];

    private Image[] _segs = new Image[84]; // 84 segmentos para C1–B7
    private Coroutine[] _activeCoroutines = new Coroutine[84];

    private void Awake()
    {
        // Define as cores das notas no modo cromático
        _noteColors[0] = new Color(1f, 0.0f, 0.0f); // C - Vermelho
        _noteColors[2] = new Color(1f, 0.5f, 0.0f); // D - Laranja
        _noteColors[4] = new Color(1f, 1.0f, 0.0f); // E - Amarelo
        _noteColors[5] = new Color(0.5f, 1.0f, 0.0f); // F - Verde Claro
        _noteColors[7] = new Color(0.0f, 1.0f, 0.0f); // G - Verde
        _noteColors[9] = new Color(0.0f, 0.5f, 1.0f); // A - Azul
        _noteColors[11] = new Color(0.5f, 0.0f, 1.0f); // B - Violeta

        _noteColors[1] = Color.Lerp(_noteColors[0], _noteColors[2], 0.5f); // C#
        _noteColors[3] = Color.Lerp(_noteColors[2], _noteColors[4], 0.5f); // D#
        _noteColors[6] = Color.Lerp(_noteColors[5], _noteColors[7], 0.5f); // F#
        _noteColors[8] = Color.Lerp(_noteColors[7], _noteColors[9], 0.5f); // G#
        _noteColors[10] = Color.Lerp(_noteColors[9], _noteColors[11], 0.5f); // A#

        // Define atrasos padrão se não definidos
        if (_ringDelays.Length < 7) _ringDelays = new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
        if (_noteDelays.Length < 12) _noteDelays = new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
    }

    private void Start()
    {
        foreach (Transform child in _ringsParent)
            Destroy(child.gameObject);

        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int segIndex = 0;

        // Cria segmentos apenas para o alcance C1 a B7 (MIDI 24 a 107)
        for (int midiNote = 24; midiNote <= 107; midiNote++, segIndex++)
        {
            int octave = (midiNote / 12) - 1;
            int noteInOctave = midiNote % 12;

            string segName = $"Seg{octave:00}[{noteNames[noteInOctave]}{octave}]";
            GameObject segGO = new GameObject(segName, typeof(RectTransform), typeof(Image));
            segGO.transform.SetParent(_ringsParent, false);

            Image img = segGO.GetComponent<Image>();
            img.fillAmount = 1f;
            if (_segSprite != null) img.sprite = _segSprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.rectTransform.sizeDelta = new Vector2(32, 32);

            // Define a cor inicial para um estado "apagado" com base no modo
            Color initialColor;
            if (_currentColorMode == ColorMode.ByRing || _currentColorMode == ColorMode.AcrossOctaves || _currentColorMode == ColorMode.FullRing)
            {
                int ring = Mathf.Clamp(octave - 1, 0, _ringColors.Length - 1);
                initialColor = _ringColors[ring] * _dimIntensity;
            }
            else
            {
                initialColor = _noteColors[noteInOctave] * _dimIntensity;
            }
            img.color = initialColor;
            _segs[segIndex] = img;
        }

        // Define o modo inicial de reprodução
        SetPlaybackMode(_currentPlaybackMode);
    }

    private void SetPlaybackMode(PlaybackMode mode)
    {
        // Desativa todos os componentes primeiro
        if (_midiFilePlayer != null) _midiFilePlayer.gameObject.SetActive(false);
        if (_midiStreamPlayer != null) _midiStreamPlayer.gameObject.SetActive(false);
        if (_baluAudioReactive != null) _baluAudioReactive.gameObject.SetActive(false);
        if (_audioSource != null) _audioSource.gameObject.SetActive(false);

        // Remove todos os listeners antes de adicionar novos
        if (_midiFilePlayer != null)
        {
            _midiFilePlayer.OnEventNotesMidi.RemoveListener(OnNotesMidi);
        }

        switch (mode)
        {
            case PlaybackMode.File:
                if (_midiFilePlayer != null)
                {
                    _midiFilePlayer.gameObject.SetActive(true);
                    _midiFilePlayer.OnEventNotesMidi.AddListener(OnNotesMidi);
                }
                break;
            case PlaybackMode.Stream:
                // Ativa o MidiStreamPlayer para reproduzir o som
                if (_midiStreamPlayer != null)
                {
                    _midiStreamPlayer.gameObject.SetActive(true);
                }
                break;
            case PlaybackMode.AudioSource:
                if (_baluAudioReactive != null)
                {
                    _baluAudioReactive.gameObject.SetActive(true);
                }
                if (_audioSource != null)
                {
                    _audioSource.gameObject.SetActive(true);
                }
                break;
        }
    }

    private void OnNotesMidi(List<MPTKEvent> noteEvents)
    {
        foreach (var noteEvent in noteEvents)
        {
            HandleNoteOn(noteEvent);
        }
    }

    // Este método agora lida com eventos do MidiInReader, que é o componente correto para entrada MIDI
    private void OnInputMidi(MPTKEvent noteEvent)
    {
        // Apenas processa se for uma nota tocada
        if (noteEvent.Command == MPTKCommand.NoteOn)
        {
            HandleNoteOn(noteEvent);
        }

        // Também é importante reproduzir o som via o MidiStreamPlayer
        if (_midiStreamPlayer != null)
        {
            _midiStreamPlayer.MPTK_PlayEvent(noteEvent);
        }
    }

    public void HandleNoteOn(MPTKEvent noteEvent)
    {
        try
        {
            int midiNote = noteEvent.Value;

            // Clampa ou ignora notas fora do alcance C1-B7
            if (_clampNotes)
            {
                if (midiNote < 24) midiNote = 24;
                if (midiNote > 107) midiNote = 107;
            }
            else
            {
                if (midiNote < 24 || midiNote > 107) return;
            }

            int segIndex = midiNote - 24;
            int octave = (midiNote / 12) - 1;
            int noteInOctave = midiNote % 12;
            int ring = Mathf.Clamp(octave - 1, 0, _ringColors.Length - 1);

            Color baseColor;
            float intensity = Mathf.Clamp01(noteEvent.Velocity / 127f);

            // Determina o modo de cor e aplica o efeito
            if (_currentColorMode == ColorMode.ByRing)
            {
                baseColor = _ringColors[ring];

                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                Color dimColor = baseColor * _dimIntensity;

                if (_activeCoroutines[segIndex] != null) StopCoroutine(_activeCoroutines[segIndex]);
                _activeCoroutines[segIndex] = StartCoroutine(LightSegment(_segs[segIndex], peakColor, dimColor, _ringDelays[ring] + _noteDelays[noteInOctave]));
            }
            else if (_currentColorMode == ColorMode.ByNote)
            {
                baseColor = _noteColors[noteInOctave];

                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                Color dimColor = baseColor * _dimIntensity;

                if (_activeCoroutines[segIndex] != null) StopCoroutine(_activeCoroutines[segIndex]);
                _activeCoroutines[segIndex] = StartCoroutine(LightSegment(_segs[segIndex], peakColor, dimColor, _ringDelays[ring] + _noteDelays[noteInOctave]));
            }
            else if (_currentColorMode == ColorMode.FullRing)
            {
                baseColor = _ringColors[ring];

                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                Color dimColor = baseColor * _dimIntensity;

                int startMidi = ((octave + 1) * 12);
                int endMidi = ((octave + 2) * 12) - 1;

                for (int m = startMidi; m <= endMidi; m++)
                {
                    int ringSegIndex = m - 24; // Atualizado para o novo índice de base
                    if (ringSegIndex >= 0 && ringSegIndex < _segs.Length)
                    {
                        if (_activeCoroutines[ringSegIndex] != null) StopCoroutine(_activeCoroutines[ringSegIndex]);
                        _activeCoroutines[ringSegIndex] = StartCoroutine(LightSegment(_segs[ringSegIndex], peakColor, dimColor, _ringDelays[ring] + _noteDelays[noteInOctave]));
                    }
                }
            }
            else if (_currentColorMode == ColorMode.RandomNoteColor)
            {
                baseColor = _noteColors[Random.Range(0, _noteColors.Length)];
                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                Color dimColor = baseColor * _dimIntensity;

                if (_activeCoroutines[segIndex] != null) StopCoroutine(_activeCoroutines[segIndex]);
                _activeCoroutines[segIndex] = StartCoroutine(LightSegment(_segs[segIndex], peakColor, dimColor, _ringDelays[ring] + _noteDelays[noteInOctave]));
            }
            else if (_currentColorMode == ColorMode.FullBoardPulse)
            {
                Color peakColor = Color.Lerp(Color.black, _noteColors[noteInOctave], intensity);

                for (int i = 0; i < _segs.Length; i++)
                {
                    if (_activeCoroutines[i] != null) StopCoroutine(_activeCoroutines[i]);

                    Color originalDimColor = GetInitialDimColor(i);
                    _segs[i].color = peakColor; // Instantaneamente acende
                    _activeCoroutines[i] = StartCoroutine(LightSegment(_segs[i], peakColor, originalDimColor, _pulseDuration));
                }
            }
            else if (_currentColorMode == ColorMode.Strobe)
            {
                baseColor = _noteColors[noteInOctave];
                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                StartCoroutine(StrobeEffect(segIndex, peakColor));
            }
            else if (_currentColorMode == ColorMode.AcrossOctaves)
            {
                // Pega a cor base do anel da nota tocada
                baseColor = _ringColors[ring];

                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                Color dimColor = baseColor * _dimIntensity;

                // Itera por todos os segmentos
                for (int i = 0; i < _segs.Length; i++)
                {
                    int currentMidiNote = i + 24;
                    int currentNoteInOctave = currentMidiNote % 12;

                    // Se a nota é a mesma, acende o segmento
                    if (currentNoteInOctave == noteInOctave)
                    {
                        if (_activeCoroutines[i] != null)
                        {
                            StopCoroutine(_activeCoroutines[i]);
                        }
                        _segs[i].color = peakColor;
                        _activeCoroutines[i] = StartCoroutine(LightSegment(_segs[i], peakColor, dimColor, _ringDelays[ring] + _noteDelays[noteInOctave]));
                    }
                }
            }
            else if (_currentColorMode == ColorMode.VerticalColor)
            {
                // Pega a cor base da nota cromática
                baseColor = _noteColors[noteInOctave];

                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                Color dimColor = baseColor * _dimIntensity;

                // Itera por todos os segmentos
                for (int i = 0; i < _segs.Length; i++)
                {
                    int currentMidiNote = i + 24;
                    int currentNoteInOctave = currentMidiNote % 12;

                    // Se a nota é a mesma, acende o segmento
                    if (currentNoteInOctave == noteInOctave)
                    {
                        if (_activeCoroutines[i] != null)
                        {
                            StopCoroutine(_activeCoroutines[i]);
                        }
                        _segs[i].color = peakColor;
                        _activeCoroutines[i] = StartCoroutine(LightSegment(_segs[i], peakColor, dimColor, _ringDelays[ring] + _noteDelays[noteInOctave]));
                    }
                }
            }
            else if (_currentColorMode == ColorMode.Cross)
            {
                // Efeito do modo FullRing (acende o anel com a cor do anel)
                Color ringPeakColor = Color.Lerp(Color.black, _ringColors[ring], intensity);
                Color ringDimColor = _ringColors[ring] * _dimIntensity;

                int startMidi = ((octave + 1) * 12);
                int endMidi = ((octave + 2) * 12) - 1;

                for (int m = startMidi; m <= endMidi; m++)
                {
                    int ringSegIndex = m - 24;
                    if (ringSegIndex >= 0 && ringSegIndex < _segs.Length)
                    {
                        if (_activeCoroutines[ringSegIndex] != null) StopCoroutine(_activeCoroutines[ringSegIndex]);
                        _segs[ringSegIndex].color = ringPeakColor;
                        _activeCoroutines[ringSegIndex] = StartCoroutine(LightSegment(_segs[ringSegIndex], ringPeakColor, ringDimColor, _ringDelays[ring] + _noteDelays[noteInOctave]));
                    }
                }

                // Efeito do modo AcrossOctaves (acende notas de mesmo nome com a cor do anel)
                for (int i = 0; i < _segs.Length; i++)
                {
                    int currentMidiNote = i + 24;
                    int currentNoteInOctave = currentMidiNote % 12;
                    int currentOctave = (currentMidiNote / 12) - 1;

                    // Ignora a oitava atual para evitar sobreposição
                    if (currentNoteInOctave == noteInOctave && currentOctave != octave)
                    {
                        if (_activeCoroutines[i] != null) StopCoroutine(_activeCoroutines[i]);
                        _segs[i].color = ringPeakColor;
                        _activeCoroutines[i] = StartCoroutine(LightSegment(_segs[i], ringPeakColor, ringDimColor, _ringDelays[ring] + _noteDelays[noteInOctave]));
                    }
                }
            }


            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            string noteName = noteNames[midiNote % 12];
            double frequency = 440.0 * System.Math.Pow(2, (midiNote - 69) / 12.0);
            _text.text = $"Nota: {noteName}{octave} | MIDI: {midiNote} | Velocidade: {noteEvent.Velocity} | Freq: {frequency:F2} Hz";
            Debug.Log($"Nota: {noteName}{octave} | MIDI: {midiNote} | Oitava: {octave} | Frequência: {frequency:F2} Hz");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Erro em HandleNoteOn: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // Método auxiliar para obter a cor de repouso de um segmento
    private Color GetInitialDimColor(int segIndex)
    {
        int midiNote = segIndex + 24;
        int octave = (midiNote / 12) - 1;
        int noteInOctave = midiNote % 12;
        int ring = Mathf.Clamp(octave - 1, 0, _ringColors.Length - 1);

        Color initialColor;
        if (_currentColorMode == ColorMode.ByRing || _currentColorMode == ColorMode.AcrossOctaves || _currentColorMode == ColorMode.FullRing)
        {
            initialColor = _ringColors[ring] * _dimIntensity;
        }
        else
        {
            initialColor = _noteColors[noteInOctave] * _dimIntensity;
        }
        return initialColor;
    }

    private IEnumerator StrobeEffect(int segIndex, Color peakColor)
    {
        if (segIndex >= 0 && segIndex < _segs.Length)
        {
            if (_activeCoroutines[segIndex] != null)
            {
                StopCoroutine(_activeCoroutines[segIndex]);
            }

            float timer = 0f;
            while (timer < _strobeDuration)
            {
                _segs[segIndex].color = peakColor;
                yield return new WaitForSeconds(0.05f);
                _segs[segIndex].color = Color.black;
                yield return new WaitForSeconds(0.05f);
                timer += 0.1f;
            }

            Color initialColor;
            int midiNote = segIndex + 24;
            int octave = (midiNote / 12) - 1;
            int noteInOctave = midiNote % 12;
            int ring = Mathf.Clamp(octave - 1, 0, _ringColors.Length - 1);
            float fadeDuration = _ringDelays[ring] + _noteDelays[noteInOctave];

            if (_currentColorMode == ColorMode.ByRing || _currentColorMode == ColorMode.AcrossOctaves || _currentColorMode == ColorMode.FullRing)
            {
                initialColor = _ringColors[ring] * _dimIntensity;
            }
            else
            {
                initialColor = _noteColors[noteInOctave] * _dimIntensity;
            }

            _segs[segIndex].color = initialColor;
            _activeCoroutines[segIndex] = StartCoroutine(LightSegment(_segs[segIndex], _segs[segIndex].color, initialColor, fadeDuration));
        }
    }

    private IEnumerator LightSegment(Image seg, Color startColor, Color endColor, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            seg.color = Color.Lerp(startColor, endColor, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        seg.color = endColor;
    }
 }
