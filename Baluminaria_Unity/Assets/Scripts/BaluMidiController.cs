using UnityEngine;
using MidiPlayerTK;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class BaluMidiController : MonoBehaviour
{
    #region Enums and Serialized Fields
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
        FullRing,
        RandomNoteColor,
        FullBoardPulse,
        Strobe,
        AcrossOctaves,
        VerticalColor,
        TwoPaths,
        OctavesBattle
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
    [SerializeField] private PlaybackMode _currentPlaybackMode = PlaybackMode.File;
    [SerializeField] private bool _clampNotes = true;

    [Header("Modo de Cores")]
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

    [SerializeField]
    private float _extraFadeOutTime = 2f; // Tempo extra para suavizar o fade-out
    [Header("Atrasos")]
    [SerializeField] private float[] _ringDelays = new float[7];
    [SerializeField] private float[] _noteDelays = new float[12];
    #endregion

    private class ActiveFade
    {
        public Image Segment;
        public Color StartColor;
        public Color EndColor;
        public float Duration;
        public float Timer;
    }

    private List<ActiveFade> _activeFades = new List<ActiveFade>();
    private Image[] _segs = new Image[84];
    private Coroutine[] _activeCoroutines = new Coroutine[84];

    private bool _isSustainPedalDown = false;
    private HashSet<int> _sustainedNotes = new HashSet<int>();

    private void Awake()
    {
        // Awake permanece o mesmo
        #region Awake Content
        _noteColors[0] = new Color(1f, 0.0f, 0.0f); // C
        _noteColors[2] = new Color(1f, 0.5f, 0.0f); // D
        _noteColors[4] = new Color(1f, 1.0f, 0.0f); // E
        _noteColors[5] = new Color(0.5f, 1.0f, 0.0f); // F
        _noteColors[7] = new Color(0.0f, 1.0f, 0.0f); // G
        _noteColors[9] = new Color(0.0f, 0.5f, 1.0f); // A
        _noteColors[11] = new Color(0.5f, 0.0f, 1.0f); // B
        _noteColors[1] = Color.Lerp(_noteColors[0], _noteColors[2], 0.5f);
        _noteColors[3] = Color.Lerp(_noteColors[2], _noteColors[4], 0.5f);
        _noteColors[6] = Color.Lerp(_noteColors[5], _noteColors[7], 0.5f);
        _noteColors[8] = Color.Lerp(_noteColors[7], _noteColors[9], 0.5f);
        _noteColors[10] = Color.Lerp(_noteColors[9], _noteColors[11], 0.5f);

        if (_ringDelays.Length < 7) _ringDelays = new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
        if (_noteDelays.Length < 12) _noteDelays = new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
        #endregion
    }

    private void Start()
    {
        // Start permanece o mesmo
        #region Start Content
        foreach (Transform child in _ringsParent) Destroy(child.gameObject);
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int segIndex = 0;
        for (int midiNote = 24; midiNote <= 107; midiNote++, segIndex++)
        {
            int octave = (midiNote / 12) - 1;
            int noteInOctave = midiNote % 12;
            string segName = $"Seg{octave:00}[{noteNames[noteInOctave]}{octave}]";
            GameObject segGO = new GameObject(segName, typeof(RectTransform), typeof(Image));
            segGO.transform.SetParent(_ringsParent, false);
            Image img = segGO.GetComponent<Image>();
            if (_segSprite != null) img.sprite = _segSprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.rectTransform.sizeDelta = new Vector2(32, 32);
            img.color = GetInitialDimColor(segIndex);
            _segs[segIndex] = img;
        }
        SetPlaybackMode(_currentPlaybackMode);
        #endregion
    }

    private void Update()
    {
        // O Update para gerenciar fades continua igual e correto
        for (int i = _activeFades.Count - 1; i >= 0; i--)
        {
            var fade = _activeFades[i];
            fade.Timer += Time.deltaTime;
            if (fade.Timer >= fade.Duration)
            {
                fade.Segment.color = fade.EndColor;
                _activeFades.RemoveAt(i);
            }
            else
            {
                float progress = fade.Timer / fade.Duration;
                fade.Segment.color = Color.Lerp(fade.StartColor, fade.EndColor, progress);
            }
        }
    }

    private void StartFade(Image segment, Color startColor, Color endColor, float duration)
    {
        // O método StartFade continua igual e correto
        int segIndex = System.Array.IndexOf(_segs, segment);
        if (segIndex != -1 && _activeCoroutines[segIndex] != null)
        {
            StopCoroutine(_activeCoroutines[segIndex]);
            _activeCoroutines[segIndex] = null;
        }
        _activeFades.RemoveAll(f => f.Segment == segment);

        // Se a duração for zero, aplica a cor final imediatamente e não adiciona à lista
        if (duration <= 0)
        {
            segment.color = endColor;
            return;
        }

        _activeFades.Add(new ActiveFade
        {
            Segment = segment,
            StartColor = startColor,
            EndColor = endColor,
            Duration = duration,
            Timer = 0f
        });
        segment.color = startColor;
    }

    public void HandleSustainPedal(bool isDown)
    {
        // A lógica do sustain continua igual e correta
        _isSustainPedalDown = isDown;
        if (!isDown)
        {
            foreach (int midiNote in _sustainedNotes)
            {
                HandleNoteOn(midiNote, 0);
            }
            _sustainedNotes.Clear();
        }
    }

    public void HandleNoteOn(int midiNote, int velocity)
    {
        MPTKEvent ev = new MPTKEvent { Command = MPTKCommand.NoteOn, Value = midiNote, Velocity = Mathf.Clamp(velocity, 0, 127) };
        HandleNoteOn(ev);
    }

    public void HandleNoteOn(MPTKEvent noteEvent)
    {
        bool isNoteOff = noteEvent.Velocity == 0;

        // Se for um Note Off e o pedal de sustain estiver pressionado, armazena a nota e ignora o resto.
        if (isNoteOff && _isSustainPedalDown)
        {
            _sustainedNotes.Add(noteEvent.Value);
        }

        try
        {
            int midiNote = noteEvent.Value;
            if (_clampNotes)
            {
                midiNote = Mathf.Clamp(midiNote, 24, 107);
            }
            // Se _clampNotes for false, a nota ainda precisa estar dentro do range válido para ser processada.
            else if (midiNote < 24 || midiNote > 107) return;

            int segIndex = midiNote - 24;
            int octave = (midiNote / 12) - 1;
            int noteInOctave = midiNote % 12;
            int ring = Mathf.Clamp(octave - 1, 0, _ringColors.Length - 1);

            // --- LÓGICA CORRIGIDA ---
            // Separa completamente o que fazer ao PRESSIONAR e ao SOLTAR a tecla.
            if (isNoteOff)
            {
                // ### EVENTO DE NOTE OFF (Soltar a tecla) ###
                // A única responsabilidade aqui é iniciar o fade-out.
                float fadeDuration = _ringDelays[ring] + _noteDelays[noteInOctave];

                // Aplica o fade de acordo com o modo de cor atual
                ApplyFadeOutByColorMode(midiNote, segIndex, ring, noteInOctave, fadeDuration);
            }
            else
            {
                // ### EVENTO DE NOTE ON (Pressionar a tecla) ###
                // A responsabilidade aqui é acender o LED com a cor certa e mantê-lo aceso.
                float intensity = Mathf.Clamp01(noteEvent.Velocity / 127f);

                // Aplica a cor de acordo com o modo atual
                ApplyLightUpByColorMode(midiNote, segIndex, ring, noteInOctave, intensity);
            }

            // Atualização de texto e debug continuam iguais
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            string noteName = noteNames[noteInOctave];
            double frequency = 440.0 * System.Math.Pow(2, (midiNote - 69) / 12.0);
            _text.text = $"Nota: {noteName}{octave} | MIDI: {midiNote} | Velocidade: {noteEvent.Velocity} | Freq: {frequency:F2} Hz";
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Erro em HandleNoteOn: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // NOVO: Método auxiliar para acender os segmentos no Note On
    private void ApplyLightUpByColorMode(int midiNote, int segIndex, int ring, int noteInOctave, float intensity)
    {
        Color baseColor, peakColor;

        // Para a maioria dos modos, cancelamos qualquer fade que esteja ocorrendo no segmento atual.
        if (_currentColorMode != ColorMode.FullBoardPulse && _currentColorMode != ColorMode.Strobe)
        {
            _activeFades.RemoveAll(f => f.Segment == _segs[segIndex]);
        }

        // A lógica para cada modo de cor define a cor e a aplica diretamente.
        switch (_currentColorMode)
        {
            case ColorMode.ByRing:
                baseColor = _ringColors[ring];
                peakColor = Color.Lerp(Color.black, baseColor, intensity);
                _segs[segIndex].color = peakColor;
                break;

            case ColorMode.ByNote:
                baseColor = _noteColors[noteInOctave];
                peakColor = Color.Lerp(Color.black, baseColor, intensity);
                _segs[segIndex].color = peakColor;
                break;

            case ColorMode.AcrossOctaves:
                baseColor = _noteColors[noteInOctave];
                peakColor = Color.Lerp(Color.black, baseColor, intensity);
                for (int i = 0; i < _segs.Length; i++)
                {
                    int currentNoteInOctave = (i + 24) % 12;
                    if (currentNoteInOctave == noteInOctave)
                    {
                        _activeFades.RemoveAll(f => f.Segment == _segs[i]);
                        _segs[i].color = peakColor;
                    }
                }
                break;

            case ColorMode.VerticalColor:
                baseColor = _ringColors[ring];
                peakColor = Color.Lerp(Color.black, baseColor, intensity);
                for (int i = 0; i < _segs.Length; i++)
                {
                    int currentNoteInOctave = (i + 24) % 12;
                    if (currentNoteInOctave == noteInOctave)
                    {
                        _activeFades.RemoveAll(f => f.Segment == _segs[i]);
                        _segs[i].color = peakColor;
                    }
                }
                break;

            case ColorMode.TwoPaths:
            case ColorMode.OctavesBattle:
                if (_currentColorMode == ColorMode.TwoPaths)
                {
                    baseColor = _noteColors[noteInOctave];
                }
                else
                {
                    baseColor = _noteColors[ring];
                }

                peakColor = Color.Lerp(Color.black, baseColor, intensity);
                // Acende a linha vertical (AcrossOctaves)
                for (int i = 0; i < _segs.Length; i++)
                {
                    int currentNoteInOctave = (i + 24) % 12;
                    if (currentNoteInOctave == noteInOctave)
                    {
                        _activeFades.RemoveAll(f => f.Segment == _segs[i]);
                        _segs[i].color = peakColor;
                    }
                }
                // Acende o anel horizontal (FullRing)
                int startMidi = ((ring + 2) * 12);
                int endMidi = startMidi + 11;
                for (int m = startMidi; m <= endMidi; m++)
                {
                    int currentSegIndex = m - 24;
                    if (currentSegIndex >= 0 && currentSegIndex < _segs.Length)
                    {
                        _activeFades.RemoveAll(f => f.Segment == _segs[currentSegIndex]);
                        _segs[currentSegIndex].color = peakColor;
                    }
                }
                break;

            case ColorMode.FullRing:
                baseColor = _ringColors[ring];
                peakColor = Color.Lerp(Color.black, baseColor, intensity);
                int startMidiFullRing = ((ring + 2) * 12);
                int endMidiFullRing = startMidiFullRing + 11;
                for (int m = startMidiFullRing; m <= endMidiFullRing; m++)
                {
                    int currentSegIndex = m - 24;
                    if (currentSegIndex >= 0 && currentSegIndex < _segs.Length)
                    {
                        _activeFades.RemoveAll(f => f.Segment == _segs[currentSegIndex]);
                        _segs[currentSegIndex].color = peakColor;
                    }
                }
                break;

            case ColorMode.Strobe:
                baseColor = _noteColors[noteInOctave];
                peakColor = Color.Lerp(Color.black, baseColor, intensity);
                StartCoroutine(StrobeEffect(segIndex, peakColor));
                break;

            case ColorMode.FullBoardPulse:
                peakColor = Color.Lerp(Color.black, _noteColors[noteInOctave], intensity);
                for (int i = 0; i < _segs.Length; i++)
                {
                    _activeFades.RemoveAll(f => f.Segment == _segs[i]);
                    _segs[i].color = peakColor;
                }
                break;

            case ColorMode.RandomNoteColor:
                peakColor = Color.Lerp(Color.black, new Color(Random.value, Random.value, Random.value), intensity);
                _segs[segIndex].color = peakColor;
                break;
        }
    }

    // NOVO: Método auxiliar para iniciar o fade-out dos segmentos no Note Off
    private void ApplyFadeOutByColorMode(int midiNote, int segIndex, int ring, int noteInOctave, float fadeDuration)
    {
        Color dimColor;
        fadeDuration = _isSustainPedalDown ? fadeDuration + _extraFadeOutTime : fadeDuration;

        switch (_currentColorMode)
        {
            case ColorMode.ByRing:
            case ColorMode.FullRing:
                dimColor = _ringColors[ring] * _dimIntensity;
                StartFade(_segs[segIndex], _segs[segIndex].color, dimColor, fadeDuration);
                if (_currentColorMode == ColorMode.FullRing)
                {
                    int startMidiFullRing = ((ring + 2) * 12);
                    int endMidiFullRing = startMidiFullRing + 11;
                    for (int m = startMidiFullRing; m <= endMidiFullRing; m++)
                    {
                        int currentSegIndex = m - 24;
                        if (currentSegIndex >= 0 && currentSegIndex < _segs.Length)
                        {
                            StartFade(_segs[currentSegIndex], _segs[currentSegIndex].color, dimColor, fadeDuration);
                        }
                    }
                }
                break;

            case ColorMode.ByNote:
            case ColorMode.RandomNoteColor:
                dimColor = _noteColors[noteInOctave] * _dimIntensity;
                StartFade(_segs[segIndex], _segs[segIndex].color, dimColor, fadeDuration);
                break;

            case ColorMode.AcrossOctaves:
                dimColor = _noteColors[noteInOctave] * _dimIntensity;
                for (int i = 0; i < _segs.Length; i++)
                {
                    int currentNoteInOctave = (i + 24) % 12;
                    if (currentNoteInOctave == noteInOctave)
                    {
                        StartFade(_segs[i], _segs[i].color, dimColor, fadeDuration);
                    }
                }
                break;

            case ColorMode.VerticalColor:
                dimColor = _ringColors[ring] * _dimIntensity;
                for (int i = 0; i < _segs.Length; i++)
                {
                    int currentNoteInOctave = (i + 24) % 12;
                    if (currentNoteInOctave == noteInOctave)
                    {
                        StartFade(_segs[i], _segs[i].color, dimColor, fadeDuration);
                    }
                }
                break;

            case ColorMode.TwoPaths:
            case ColorMode.OctavesBattle:
                if (_currentColorMode == ColorMode.TwoPaths)
                {
                    dimColor = _noteColors[noteInOctave] * _dimIntensity;
                }
                else
                {
                    dimColor = _noteColors[ring] * _dimIntensity;
                }
                // Fade-out da linha vertical
                for (int i = 0; i < _segs.Length; i++)
                {
                    int currentNoteInOctave = (i + 24) % 12;
                    if (currentNoteInOctave == noteInOctave)
                    {
                        StartFade(_segs[i], _segs[i].color, dimColor, fadeDuration);
                    }
                }
                // Fade-out do anel horizontal
                int startMidiTwoPaths = ((ring + 2) * 12);
                int endMidiTwoPaths = startMidiTwoPaths + 11;
                for (int m = startMidiTwoPaths; m <= endMidiTwoPaths; m++)
                {
                    int currentSegIndex = m - 24;
                    if (currentSegIndex >= 0 && currentSegIndex < _segs.Length)
                    {
                        StartFade(_segs[currentSegIndex], _segs[currentSegIndex].color, dimColor, fadeDuration);
                    }
                }
                break;

            case ColorMode.FullBoardPulse:
                for (int i = 0; i < _segs.Length; i++)
                {
                    Color initialDimColor = GetInitialDimColor(i);
                    StartFade(_segs[i], _segs[i].color, initialDimColor, fadeDuration);
                }
                break;
        }
    }

    private IEnumerator StrobeEffect(int segIndex, Color peakColor)
    {
        if (segIndex < 0 || segIndex >= _segs.Length) yield break;
        _activeFades.RemoveAll(f => f.Segment == _segs[segIndex]);
        if (_activeCoroutines[segIndex] != null) StopCoroutine(_activeCoroutines[segIndex]);

        IEnumerator strobe()
        {
            float timer = 0f;
            while (timer < _strobeDuration)
            {
                _segs[segIndex].color = peakColor;
                yield return new WaitForSeconds(0.05f);
                _segs[segIndex].color = Color.black;
                yield return new WaitForSeconds(0.05f);
                timer += 0.1f;
            }
            Color finalColor = GetInitialDimColor(segIndex);
            float fadeDuration = _ringDelays[0];
            StartFade(_segs[segIndex], _segs[segIndex].color, finalColor, fadeDuration);
            _activeCoroutines[segIndex] = null;
        }
        _activeCoroutines[segIndex] = StartCoroutine(strobe());
    }

    #region Helper Methods
    private Color GetInitialDimColor(int segIndex)
    {
        int midiNote = segIndex + 24;
        int octave = (midiNote / 12) - 1;
        int noteInOctave = midiNote % 12;
        int ring = Mathf.Clamp(octave - 1, 0, _ringColors.Length - 1);
        Color initialColor;

        switch (_currentColorMode)
        {
            case ColorMode.ByRing:
            case ColorMode.VerticalColor:
            case ColorMode.FullRing:
                initialColor = _ringColors[ring] * _dimIntensity;
                break;
            case ColorMode.ByNote:
            case ColorMode.AcrossOctaves:
            case ColorMode.TwoPaths:
            case ColorMode.RandomNoteColor:
            case ColorMode.FullBoardPulse:
            case ColorMode.Strobe:
                initialColor = _noteColors[noteInOctave] * _dimIntensity;
                break;
            default:
                initialColor = Color.black * _dimIntensity;
                break;
        }
        return initialColor;
    }

    private void OnNotesMidi(List<MPTKEvent> noteEvents)
    {
        foreach (var noteEvent in noteEvents)
        {
            HandleNoteOn(noteEvent);
        }
    }

    private void SetPlaybackMode(PlaybackMode mode)
    {
        if (_midiFilePlayer != null) _midiFilePlayer.gameObject.SetActive(false);
        if (_midiStreamPlayer != null) _midiStreamPlayer.gameObject.SetActive(false);
        if (_baluAudioReactive != null) _baluAudioReactive.gameObject.SetActive(false);
        if (_audioSource != null) _audioSource.gameObject.SetActive(false);
        if (_midiFilePlayer != null) _midiFilePlayer.OnEventNotesMidi.RemoveListener(OnNotesMidi);

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
                if (_midiStreamPlayer != null) _midiStreamPlayer.gameObject.SetActive(true);
                break;
            case PlaybackMode.AudioSource:
                if (_baluAudioReactive != null) _baluAudioReactive.gameObject.SetActive(true);
                if (_audioSource != null) _audioSource.gameObject.SetActive(true);
                break;
        }
    }
    #endregion
}
