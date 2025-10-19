using MidiPlayerTK;
using MPTK.NAudio.Midi;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BaluMidiController : MonoBehaviour
{
    #region Enums
    // Enum para a Tonalidade (Key Note - MIDI 0 a 11)
    public enum KeyNote
    {
        C = 0, CSharp = 1, D = 2, DSharp = 3, E = 4, F = 5,
        FSharp = 6, G = 7, GSharp = 8, A = 9, ASharp = 10, B = 11
    }

    // Enum para os 7 Modos Gregos (Escalas de 7 notas)
    public enum ScaleMode
    {
        Ionian,      // Jônio (Maior Natural)
        Dorian,      // Dórico
        Phrygian,    // Frígio
        Lydian,      // Lídio
        Mixolydian,  // Mixolídio
        Aeolian,     // Eólio (Menor Natural)
        Locrian      // Lócrio
    }

    // Enum to control playback mode
    public enum PlaybackMode
    {
        File, Stream, AudioSource
    }

    // NOVO: Enum para a Fonte de Cor do Pulso do Separador
    public enum SeparatorColorSource
    {
        SelectedColor, // Usar a cor definida em _separatorPulseColor
        KeyColor,      // Usar a cor cromática da nota tocada
        RingColor      // Usar a cor do anel/oitava tocada
    }

    // ENUM ATUALIZADO E RENOMEADO PARA CLAREZA (Grau: Tonal, Anel: Não-Tonal)
    public enum ColorMode
    {
        Note,              // Tonal: Segmento do Grau na Cor da Nota (Antigo: ByNote)
        Ring,              // Tonal: Segmento do Grau na Cor do Anel (Antigo: ByRing)
        FullRing,          // Não Tonal: Anel inteiro (16 Segs) na Cor do Anel (Antigo: FullRing)
        Random,            // Tonal: Segmento do Grau em Cor Aleatória (Antigo: RandomNoteColor)
        FullBoard,         // Não Tonal: Pulso em toda a placa (16 Segs + Separadores) (Antigo: FullBoardPulse)
        Strobe,            // Tonal: Segmento do Grau com efeito Strobe (Antigo: Strobe)
        AcrossOctaves,     // Tonal: Segmento do Grau aceso em todas as oitavas (Antigo: AcrossOctaves)
        TwoPaths,          // Híbrido: Linha (16 Segs) + Coluna (7 Graus) na Cor da Nota (Antigo: TwoPaths)
        TonalKeyPulse,     // Tonal: Segmento do Grau + Pulso do Separador na T/3/5/7 (Antigo: TonalKeyPulse)
        VerticalColor,     // VerticalColor
        OctavesBattle,     // OctavesBattle
    }

    // NEW: Enum to control output mode
    public enum OutputMode
    {
        UI, Baluminaria
    }
    #endregion

    #region Constants
    // Constantes para o Baluminaria 7x16
    private const int TOTAL_SEGMENTS = 112; // 7 * 16
    private const int SEGMENTS_PER_RING = 16;
    private const int MIDI_NOTE_MIN = 24; // C2
    private const int MIDI_NOTE_MAX = 107; // B8

    // NOVO: Índices dos separadores dentro do anel (0-15)
    private const int FIRST_SEPARATOR_INDEX = 7;
    private const int SECOND_SEPARATOR_INDEX = 15;
    #endregion

    #region MPTK Components
    [Header("MPTK Components")]
    [SerializeField] private MidiFilePlayer _midiFilePlayer;
    [SerializeField] private MidiStreamPlayer _midiStreamPlayer;
    [SerializeField] private BaluAudioReactive _baluAudioReactive;
    #endregion

    #region Information Display
    [Header("Information Display")]
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Transform _ringsParent; // Usado apenas para UI
    #endregion

    #region Output Settings
    [Header("Output Settings")]
    [SerializeField] private OutputMode _currentOutputMode = OutputMode.UI;
    [SerializeField] private GameObject _uiCanvas;
    [SerializeField] private Baluminaria _baluminaria;
    #endregion

    #region Instrument Settings
    [Header("Instrument Settings")]
    [Range(0, 127)] // MIDI Program Change vai de 0 a 127
    [SerializeField] private int _currentInstrumentProgram = 0; // 0 é geralmente Piano Acústico
    [Range(0, 15)] // Canais MIDI vão de 0 a 15
    [SerializeField] private int _midiChannel = 0; // Canal MIDI padrão
    #endregion

    #region Playback Settings
    [Header("Playback Settings")]
    [SerializeField] private PlaybackMode _currentPlaybackMode = PlaybackMode.File;
    [SerializeField] private bool _clampNotes = true;
    #endregion

    #region Analysis Settings
    [Header("Analysis Settings")]
    [SerializeField, Range(0.01f, 0.1f)] private float _analysisSensitivity = 0.03f; // Ordem invertida no Range para facilitar o entendimento (menor valor = maior sensibilidade)
    [SerializeField, Range(0.01f, 0.5f)] private float _analysisInterval = 0.01f;
    #endregion

    #region Color Mode & Visual Settings
    [Header("Color Mode")]
    [SerializeField] private ColorMode _currentColorMode = ColorMode.Ring;

    [Header("Colors by Ring (Rainbow)")]
    [SerializeField]
    public Color[] _ringColors = new Color[7]
    {
        new Color(1f, 0f, 0f),       // Red
        new Color(1f, 0.5f, 0f),     // Orange
        new Color(1f, 1f, 0f),       // Yellow
        new Color(0f, 1f, 0f),       // Green     
        new Color(0f, 0f, 1f),       // Blue
        new Color(0.29f, 0f, 0.51f), // Indigo
        new Color(0.56f, 0f, 1f)     // Violet
    };

    [Header("Colors by Note (Chromatic)")]
    [SerializeField]
    public Color[] _noteColors = new Color[12];

    [SerializeField] private Sprite _segSprite; // Usado apenas para UI

    [Header("Visual Settings")]
    [SerializeField, Range(0f, 1f)] private float _dimIntensity = 0.1f;
    [SerializeField] private float _pulseDuration = 0.1f;
    [SerializeField] private float _strobeDuration = 0.2f;
    [SerializeField] private float _extraFadeOutTime = 2f; // Extra time to smooth the fade-out
    #endregion

    #region Delays
    [Header("Delays")]
    [SerializeField] private float[] _ringDelays = new float[7];
    [SerializeField] private float[] _noteDelays = new float[12];
    #endregion

    #region Tonal Key Pulse Settings
    [Header("Tonal Key Pulse Settings (NOVO)")]
    [SerializeField] private KeyNote _tonalKey = KeyNote.C;
    [SerializeField] private ScaleMode _scaleMode = ScaleMode.Ionian;
    [SerializeField] private SeparatorColorSource _separatorColorSource = SeparatorColorSource.KeyColor;
    [SerializeField] private Color _separatorPulseColor = new Color(0.2f, 0.8f, 1.0f); // Cor de Pulso (Cyan) - Usada se SelectedColor
    [SerializeField, Range(0f, 1f)] private float _separatorDimIntensity = 0.05f; // Intensidade Dim para os separadores

    private int _tonalKeyNoteValue;
    private readonly Dictionary<ScaleMode, int[]> _scaleDegrees = new Dictionary<ScaleMode, int[]>
    {
        { ScaleMode.Ionian,      new int[] { 0, 2, 4, 5, 7, 9, 11 } }, // Maior
        { ScaleMode.Dorian,      new int[] { 0, 2, 3, 5, 7, 9, 10 } },
        { ScaleMode.Phrygian,    new int[] { 0, 1, 3, 5, 7, 8, 10 } },
        { ScaleMode.Lydian,      new int[] { 0, 2, 4, 6, 7, 9, 11 } },
        { ScaleMode.Mixolydian,  new int[] { 0, 2, 4, 5, 7, 9, 10 } },
        { ScaleMode.Aeolian,     new int[] { 0, 2, 3, 5, 7, 8, 10 } }, // Menor Natural
        { ScaleMode.Locrian,     new int[] { 0, 1, 3, 5, 6, 8, 10 } }
    };
    private readonly int[] _semitoneDistanceToDegreeIndex = { 0, 1, 1, 2, 2, 3, 4, 4, 5, 5, 6, 6 };
    private int[] _currentScale;
    #endregion

    #region Internal State & Caches
    private class ActiveFadeUI
    {
        public Image Segment;
        public Color StartColor;
        public Color EndColor;
        public float Duration;
        public float Timer;
    }

    private class ActiveFadeBaluminaria
    {
        public Segment Segment;
        public float StartIntensity;
        public float EndIntensity;
        public float Duration;
        public float Timer;
    }

    private List<ActiveFadeUI> _activeFadesUI = new List<ActiveFadeUI>();
    private List<ActiveFadeBaluminaria> _activeFadesBaluminaria = new List<ActiveFadeBaluminaria>();

    private Image[] _uiSegments = new Image[TOTAL_SEGMENTS];
    private Segment[] _baluSegments = new Segment[TOTAL_SEGMENTS]; // Mapeia para os segmentos reais da Baluminaria

    private Coroutine[] _activeCoroutines = new Coroutine[TOTAL_SEGMENTS];

    private bool _isSustainPedalDown = false;
    private HashSet<int> _sustainedNotes = new HashSet<int>();

    // Cache para o nome das notas
    private static readonly string[] _noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    // Cached boolean flags for color modes to avoid repeated checks
    private bool _isTonalMappingMode;
    private bool _isTonalKeyPulseMode;
    private bool _isAcrossOctavesMode;
    private bool _isTwoPathsMode;
    private bool _isFullRingMode;
    private bool _isFullBoardMode;
    private bool _isStrobeMode;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeChromaticColors();
        EnsureDelayArraysSize();
        CalculateCurrentScale();
        UpdateColorModeFlags(); // Initialize flags on Awake
    }

    private IEnumerator Start()
    {
        SetOutputMode(_currentOutputMode);
        yield return new WaitForSeconds(0.2f); // Pequeno atraso para garantir que tudo esteja inicializado
        SetPlaybackMode(_currentPlaybackMode);
        SetInstrument(_currentInstrumentProgram, _midiChannel);
    }

    private void OnEnable()
    {
        // Subscribe to MPTK events if already active
        if (_midiFilePlayer != null && _currentPlaybackMode == PlaybackMode.File)
        {
            _midiFilePlayer.OnEventNotesMidi.AddListener(OnNotesMidiFromFile);
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from MPTK events
        if (_midiFilePlayer != null)
        {
            _midiFilePlayer.OnEventNotesMidi.RemoveListener(OnNotesMidiFromFile);
        }
    }

    private void Update()
    {
        // Use a loop for efficiency instead of LINQ for active fades
        UpdateActiveFadesUI();
        UpdateActiveFadesBaluminaria();
    }
    #endregion

    #region Initialization Helpers
    private void InitializeChromaticColors()
    {
        // Simplificado com interpolação linear para sharps
        _noteColors[0] = new Color(1f, 0.0f, 0.0f);     // C
        _noteColors[2] = new Color(1f, 0.5f, 0.0f);     // D
        _noteColors[4] = new Color(1f, 1.0f, 0.0f);     // E
        _noteColors[5] = new Color(0.5f, 1.0f, 0.0f);   // F
        _noteColors[7] = new Color(0.0f, 1.0f, 0.0f);   // G
        _noteColors[9] = new Color(0.0f, 0.5f, 1.0f);   // A
        _noteColors[11] = new Color(0.5f, 0.0f, 1.0f);  // B

        _noteColors[1] = Color.Lerp(_noteColors[0], _noteColors[2], 0.5f); // C#
        _noteColors[3] = Color.Lerp(_noteColors[2], _noteColors[4], 0.5f); // D#
        _noteColors[6] = Color.Lerp(_noteColors[5], _noteColors[7], 0.5f); // F#
        _noteColors[8] = Color.Lerp(_noteColors[7], _noteColors[9], 0.5f); // G#
        _noteColors[10] = Color.Lerp(_noteColors[9], _noteColors[11], 0.5f); // A#
    }

    private void EnsureDelayArraysSize()
    {
        // Garante que os arrays de delays tenham o tamanho correto, preenchendo com valor padrão se menor
        if (_ringDelays.Length < 7) Array.Resize(ref _ringDelays, 7);
        if (_noteDelays.Length < 12) Array.Resize(ref _noteDelays, 12);
    }

    private void CalculateCurrentScale()
    {
        _tonalKeyNoteValue = (int)_tonalKey;

        if (!_scaleDegrees.TryGetValue(_scaleMode, out int[] baseScale))
        {
            Debug.LogError($"Scale Mode {_scaleMode} not found. Defaulting to Ionian.");
            baseScale = _scaleDegrees[ScaleMode.Ionian];
        }

        _currentScale = new int[7];
        for (int i = 0; i < 7; i++)
        {
            _currentScale[i] = (baseScale[i] + _tonalKeyNoteValue) % 12;
        }

        Debug.Log($"<color=cyan>Scale Defined: {_tonalKey} {_scaleMode}.</color>");
    }

    // New method to update boolean flags for color modes
    private void UpdateColorModeFlags()
    {
        _isTonalMappingMode = _currentColorMode == ColorMode.TonalKeyPulse ||
                              _currentColorMode == ColorMode.Note ||
                              _currentColorMode == ColorMode.Random ||
                              _currentColorMode == ColorMode.Strobe ||
                              _currentColorMode == ColorMode.Ring ||
                              _currentColorMode == ColorMode.AcrossOctaves ||
                              _currentColorMode == ColorMode.VerticalColor ||
                              _currentColorMode == ColorMode.OctavesBattle;
        _isTonalKeyPulseMode = _currentColorMode == ColorMode.TonalKeyPulse;
        _isAcrossOctavesMode = _currentColorMode == ColorMode.AcrossOctaves;
        _isTwoPathsMode = _currentColorMode == ColorMode.TwoPaths;
        _isFullRingMode = _currentColorMode == ColorMode.FullRing;
        _isFullBoardMode = _currentColorMode == ColorMode.FullBoard;
        _isStrobeMode = _currentColorMode == ColorMode.Strobe;
    }
    #endregion

    #region Fade Logic
    private void UpdateActiveFadesUI()
    {
        for (int i = _activeFadesUI.Count - 1; i >= 0; i--)
        {
            var fade = _activeFadesUI[i];
            fade.Timer += Time.deltaTime;
            if (fade.Timer >= fade.Duration)
            {
                fade.Segment.color = fade.EndColor;
                _activeFadesUI.RemoveAt(i);
            }
            else
            {
                float progress = fade.Timer / fade.Duration;
                fade.Segment.color = Color.Lerp(fade.StartColor, fade.EndColor, progress);
            }
        }
    }

    private void UpdateActiveFadesBaluminaria()
    {
        for (int i = _activeFadesBaluminaria.Count - 1; i >= 0; i--)
        {
            var fade = _activeFadesBaluminaria[i];
            fade.Timer += Time.deltaTime;
            if (fade.Timer >= fade.Duration)
            {
                fade.Segment.SetIntensity(fade.EndIntensity);
                _activeFadesBaluminaria.RemoveAt(i);
            }
            else
            {
                float progress = fade.Timer / fade.Duration;
                float currentIntensity = Mathf.Lerp(fade.StartIntensity, fade.EndIntensity, progress);
                fade.Segment.SetIntensity(currentIntensity);
            }
        }
    }

    private void StartFadeUI(Image segment, Color startColor, Color endColor, float duration)
    {
        // Optimization: Pre-allocate List.Capacity if performance bottleneck
        _activeFadesUI.RemoveAll(f => f.Segment == segment);

        if (duration <= 0)
        {
            segment.color = endColor;
            return;
        }

        _activeFadesUI.Add(new ActiveFadeUI
        {
            Segment = segment,
            StartColor = startColor,
            EndColor = endColor,
            Duration = duration,
            Timer = 0f
        });
    }

    private void StartFadeBaluminaria(Segment segment, float startIntensity, float endIntensity, float duration = 0)
    {
        // Optimization: Pre-allocate List.Capacity if performance bottleneck
        _activeFadesBaluminaria.RemoveAll(f => f.Segment == segment);

        if (duration <= 0)
        {
            segment.SetIntensity(endIntensity);
            return;
        }

        _activeFadesBaluminaria.Add(new ActiveFadeBaluminaria
        {
            Segment = segment,
            StartIntensity = startIntensity,
            EndIntensity = endIntensity,
            Duration = duration,
            Timer = 0f
        });
    }
    #endregion

    #region MIDI Event Handlers
    public void HandleNoteOn(int midiNote, int velocity)
    {
        // Reuse MPTKEvent for consistent processing
        HandleNoteEvent(new MPTKEvent { Command = MPTKCommand.NoteOn, Value = midiNote, Velocity = Mathf.Clamp(velocity, 0, 127) });
    }

    public void HandleNoteEvent(MPTKEvent noteEvent)
    {
        bool isNoteOffEvent = noteEvent.Velocity == 0;

        // Handle sustain pedal logic upfront
        if (isNoteOffEvent && _isSustainPedalDown)
        {
            _sustainedNotes.Add(noteEvent.Value);
            return; // Don't process visual fade-out yet if sustained
        }

        int midiNote = noteEvent.Value;
        if (_clampNotes)
        {
            midiNote = Mathf.Clamp(midiNote, MIDI_NOTE_MIN, MIDI_NOTE_MAX);
        }
        else if (midiNote < MIDI_NOTE_MIN || midiNote > MIDI_NOTE_MAX)
        {
            return; // Note is out of range and not clamped
        }

        int ring = (midiNote / 12) - 2; // Octave 2-8 maps to ring 0-6
        int noteInOctave = midiNote % 12;
        int octave = (midiNote / 12) - 1; // Real octave number (e.g., C2 is Octave 2)

        if (ring < 0 || ring >= _ringColors.Length)
        {
            // Debug.LogWarning($"Note {midiNote} is out of visual range for rings.");
            return;
        }

        // MPTK Sound Logic
        if (_currentPlaybackMode == PlaybackMode.Stream && _midiStreamPlayer != null)
        {
            if (noteEvent.Velocity > 0)
            {
                _sustainedNotes.Remove(noteEvent.Value);
                _midiStreamPlayer.MPTK_PlayEvent(noteEvent);
            }
            else
            {
                if (!_isSustainPedalDown || !_sustainedNotes.Contains(noteEvent.Value))
                {
                    _midiStreamPlayer.MPTK_PlayEvent(noteEvent);
                    _sustainedNotes.Remove(noteEvent.Value);
                }
            }
        }

        // Visualization Logic
        if (isNoteOffEvent)
        {
            float fadeDuration = _ringDelays[ring] + _noteDelays[noteInOctave];
            FadeOut(midiNote, ring, noteInOctave, fadeDuration);
        }
        else // It's a Note On event
        {
            float velocityIntensity = noteEvent.Velocity / 127f; // Already clamped
            LightUp(midiNote, ring, noteInOctave, velocityIntensity);
            // For File Playback, notes are usually short, so fade out immediately
            if (_currentPlaybackMode == PlaybackMode.File)
            {
                float fadeDuration = _ringDelays[ring] + _noteDelays[noteInOctave];
                FadeOut(midiNote, ring, noteInOctave, fadeDuration);
            }
        }

        // Update UI Text
        double frequency = 440.0 * System.Math.Pow(2, (midiNote - 69) / 12.0);
        _text.text = $"Nota: {_noteNames[noteInOctave]}{octave} | MIDI: {midiNote} | Velocidade: {noteEvent.Velocity} | Freq: {frequency:F2} Hz";
    }

    public void HandleSustainPedal(bool isDown)
    {
        _isSustainPedalDown = isDown;
        if (!isDown)
        {
            Debug.Log("<color=purple>Sustain Pedal RELEASED. Stopping sustained notes.</color>");
            foreach (int midiNote in _sustainedNotes)
            {
                MPTKEvent noteOffEvent = new MPTKEvent { Command = MPTKCommand.NoteOn, Value = midiNote, Velocity = 0 };
                // Send note off to MPTK stream player
                if (_currentPlaybackMode == PlaybackMode.Stream && _midiStreamPlayer != null)
                {
                    _midiStreamPlayer.MPTK_PlayEvent(noteOffEvent);
                }
                // Also trigger visual fade-out
                HandleNoteEvent(noteOffEvent);
            }
            _sustainedNotes.Clear();
        }
    }
    #endregion

    #region Visualization Core Logic (LightUp & FadeOut)

    /// <summary>
    /// Lights up the segments based on the current ColorMode.
    /// </summary>
    private void LightUp(int midiNote, int ring, int noteInOctave, float intensity)
    {
        Color baseColor;
        float targetIntensity = intensity * _baluminaria.maxIntensity; // Pre-calculate for Baluminaria

        // Local function to light a single segment (abstracts UI/Baluminaria differences)
        Action<int, Color, float> lightSegment = (index, color, targetLightIntensity) =>
        {
            if (index < 0 || index >= TOTAL_SEGMENTS) return;
            if (_currentOutputMode == OutputMode.UI)
            {
                _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[index]);
                _uiSegments[index].color = Color.Lerp(_uiSegments[index].color, color, intensity);
            }
            else
            {
                _baluSegments[index]?.ChangeLightColor(color);
                StartFadeBaluminaria(_baluSegments[index], _baluSegments[index]?.GetComponentInChildren<Light>().intensity ?? 0f, targetLightIntensity);
            }
        };

        if (_isTonalMappingMode)
        {
            // --- TONAL MODES (7 Scale Degree Segments) ---
            int degreeIndex = GetScaleDegreeIndex(noteInOctave);
            baseColor = GetTonalModeBaseColor(noteInOctave, ring);

            if (_isAcrossOctavesMode)
            {
                // Light up the scale degree vertically across all 7 rings
                for (int r = 0; r < 7; r++)
                {
                    int verticalSegIndexA = r * SEGMENTS_PER_RING + degreeIndex;
                    int verticalSegIndexB = r * SEGMENTS_PER_RING + degreeIndex + 8;
                    lightSegment(verticalSegIndexA, baseColor, targetIntensity);
                    lightSegment(verticalSegIndexB, baseColor, targetIntensity);
                }
            }
            else
            {
                // Standard Tonal Logic (only the played ring)
                (int segIndexA, int segIndexB) = GetNoteSegmentIndices(noteInOctave, ring);
                lightSegment(segIndexA, baseColor, targetIntensity);
                lightSegment(segIndexB, baseColor, targetIntensity);

                if (_isStrobeMode)
                {
                    // Start Strobe effect coroutines
                    if (_activeCoroutines[segIndexA] != null) StopCoroutine(_activeCoroutines[segIndexA]);
                    if (_activeCoroutines[segIndexB] != null) StopCoroutine(_activeCoroutines[segIndexB]);
                    _activeCoroutines[segIndexA] = StartCoroutine(StrobeEffect(segIndexA, targetIntensity, baseColor));
                    _activeCoroutines[segIndexB] = StartCoroutine(StrobeEffect(segIndexB, targetIntensity, baseColor));
                }
            }

            if (_isTonalKeyPulseMode)
            {
                // Check if the played note's degree is a key chord degree (T, 3, 5, 7)
                // Assuming _currentScale contains the pitch classes of the current scale.
                // We need to check the 'degreeIndex' relative to the scale, not just pitch class.
                // In your existing code, this check was based on 'degreeIndex': (0, 2, 4, 6)
                bool shouldPulse = (degreeIndex == 0 || degreeIndex == 2 || degreeIndex == 4 || degreeIndex == 6);

                if (shouldPulse)
                {
                    float separatorTargetIntensity = intensity * _baluminaria.maxIntensity;
                    float fadeDuration = _pulseDuration;

                    int sepIndexA = ring * SEGMENTS_PER_RING + FIRST_SEPARATOR_INDEX;
                    int sepIndexB = ring * SEGMENTS_PER_RING + SECOND_SEPARATOR_INDEX;

                    Color pulseColor = GetSeparatorPulseColor(noteInOctave, ring);

                    Action<int> pulseSeparator = (index) =>
                    {
                        if (index < 0 || index >= TOTAL_SEGMENTS) return;
                        if (_currentOutputMode == OutputMode.UI)
                        {
                            _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[index]);
                            StartFadeUI(_uiSegments[index], _uiSegments[index].color, pulseColor, fadeDuration / 2f);
                        }
                        else
                        {
                            _baluSegments[index]?.ChangeLightColor(pulseColor);
                            StartFadeBaluminaria(_baluSegments[index], 0f, separatorTargetIntensity, fadeDuration / 2f);
                            StartFadeBaluminaria(_baluSegments[index], separatorTargetIntensity, _separatorDimIntensity * _baluminaria.maxIntensity, fadeDuration);
                        }
                    };

                    pulseSeparator(sepIndexA);
                    pulseSeparator(sepIndexB);
                }
            }
        }
        else if (_isTwoPathsMode) // HYBRID LOGIC
        {
            baseColor = GetTonalModeBaseColor(noteInOctave, ring); // Uses Note Color for both lines
            int degreeIndex = GetScaleDegreeIndex(noteInOctave);

            // 1. Vertical Column (Tonal Logic: 7 Degrees)
            for (int r = 0; r < 7; r++)
            {
                int segIndexA = r * SEGMENTS_PER_RING + degreeIndex;
                int segIndexB = r * SEGMENTS_PER_RING + degreeIndex + 8;
                lightSegment(segIndexA, baseColor, targetIntensity);
                lightSegment(segIndexB, baseColor, targetIntensity);
            }

            // 2. Horizontal Line (Non-Tonal Logic: 16 Segments in the played ring)
            for (int n = 0; n < SEGMENTS_PER_RING; n++)
            {
                int currentSegIndex = ring * SEGMENTS_PER_RING + n;
                lightSegment(currentSegIndex, baseColor, targetIntensity);
            }
        }
        else
        {
            // --- NON-TONAL MODES (16 Segments) ---
            if (_isFullRingMode)
            {
                baseColor = _ringColors[ring];
                for (int n = 0; n < SEGMENTS_PER_RING; n++) // Iterate ALL 16 segments
                {
                    int currentSegIndex = ring * SEGMENTS_PER_RING + n;
                    lightSegment(currentSegIndex, baseColor, targetIntensity);
                }
            }
            else if (_isFullBoardMode)
            {
                baseColor = _noteColors[noteInOctave];
                for (int i = 0; i < TOTAL_SEGMENTS; i++) // Light up the entire board
                {
                    lightSegment(i, baseColor, targetIntensity);
                }
            }
        }
    }


    /// <summary>
    /// Fades out the segments based on the current ColorMode.
    /// </summary>
    private void FadeOut(int midiNote, int ring, int noteInOctave, float fadeDuration)
    {
        fadeDuration = _isSustainPedalDown ? fadeDuration + _extraFadeOutTime : fadeDuration;
        float dimIntensity = _dimIntensity * _baluminaria.maxIntensity;

        // Local function to fade a single segment (abstracts UI/Baluminaria differences)
        Action<int> fadeSegment = (index) =>
        {
            if (index < 0 || index >= TOTAL_SEGMENTS) return;
            Color dimColor = GetInitialDimColor(index); // Get the correct dim color for the segment
            if (_currentOutputMode == OutputMode.UI)
            {
                StartFadeUI(_uiSegments[index], _uiSegments[index].color, dimColor, fadeDuration);
            }
            else
            {
                StartFadeBaluminaria(_baluSegments[index], _baluSegments[index]?.GetComponentInChildren<Light>().intensity ?? 0f, dimIntensity, fadeDuration);
            }
        };

        if (_isTonalMappingMode)
        {
            // --- TONAL MODES (7 Scale Degree Segments) ---
            int degreeIndex = GetScaleDegreeIndex(noteInOctave);

            if (_isAcrossOctavesMode)
            {
                // Fade out the scale degree vertically across all 7 rings
                for (int r = 0; r < 7; r++)
                {
                    int tempSegIndexA = r * SEGMENTS_PER_RING + degreeIndex;
                    int tempSegIndexB = r * SEGMENTS_PER_RING + degreeIndex + 8;
                    fadeSegment(tempSegIndexA);
                    fadeSegment(tempSegIndexB);
                }
            }
            else
            {
                // Standard Tonal Fade Out (only the played ring)
                (int segIndexA, int segIndexB) = GetNoteSegmentIndices(noteInOctave, ring);
                fadeSegment(segIndexA);
                fadeSegment(segIndexB);
            }

            if (_isTonalKeyPulseMode)
            {
                int degreeIndexPulse = GetScaleDegreeIndex(noteInOctave);
                bool wasKeyChord = (degreeIndexPulse == 0 || degreeIndexPulse == 2 || degreeIndexPulse == 4 || degreeIndexPulse == 6);

                if (wasKeyChord)
                {
                    int sepIndexA = ring * SEGMENTS_PER_RING + FIRST_SEPARATOR_INDEX;
                    int sepIndexB = ring * SEGMENTS_PER_RING + SECOND_SEPARATOR_INDEX;
                    fadeSegment(sepIndexA);
                    fadeSegment(sepIndexB);
                }
            }
        }
        else if (_isTwoPathsMode) // HYBRID LOGIC
        {
            int degreeIndex = GetScaleDegreeIndex(noteInOctave);

            // 1. Vertical Column (Tonal Logic: Fade out the 7 vertical degrees)
            for (int r = 0; r < 7; r++)
            {
                int segIndexA = r * SEGMENTS_PER_RING + degreeIndex;
                int segIndexB = r * SEGMENTS_PER_RING + degreeIndex + 8;
                fadeSegment(segIndexA);
                fadeSegment(segIndexB);
            }

            // 2. Horizontal Line (Non-Tonal Logic: Fade out the 16 segments of the played ring)
            for (int n = 0; n < SEGMENTS_PER_RING; n++)
            {
                int currentSegIndex = ring * SEGMENTS_PER_RING + n;
                int segInRing = currentSegIndex % SEGMENTS_PER_RING;

                // Avoid double-fading segments that are part of the vertical degree
                bool isVerticalSegment = (segInRing == degreeIndex || segInRing == degreeIndex + 8);
                if (!isVerticalSegment)
                {
                    fadeSegment(currentSegIndex);
                }
            }
        }
        else
        {
            // --- NON-TONAL MODES (16 Segments) ---
            if (_isFullRingMode)
            {
                for (int n = 0; n < SEGMENTS_PER_RING; n++)
                {
                    int currentSegIndex = ring * SEGMENTS_PER_RING + n;
                    fadeSegment(currentSegIndex);
                }
            }
            else if (_isFullBoardMode)
            {
                // FullBoard fades the entire board
                for (int i = 0; i < TOTAL_SEGMENTS; i++)
                {
                    fadeSegment(i);
                }
            }
        }
    }

    private IEnumerator StrobeEffect(int segIndex, float targetIntensity, Color baseColor)
    {
        if (segIndex < 0 || segIndex >= TOTAL_SEGMENTS) yield break;

        // Ensure only one strobe coroutine runs per segment
        if (_activeCoroutines[segIndex] != null)
        {
            StopCoroutine(_activeCoroutines[segIndex]);
        }

        IEnumerator strobeCoroutine()
        {
            float timer = 0f;
            while (timer < _strobeDuration)
            {
                if (_currentOutputMode == OutputMode.UI)
                {
                    _uiSegments[segIndex].color = baseColor;
                }
                else
                {
                    _baluSegments[segIndex]?.SetIntensity(targetIntensity);
                }
                yield return new WaitForSeconds(0.05f);

                if (_currentOutputMode == OutputMode.UI)
                {
                    _uiSegments[segIndex].color = GetInitialDimColor(segIndex);
                }
                else
                {
                    _baluSegments[segIndex]?.SetIntensity(0);
                }
                yield return new WaitForSeconds(0.05f);
                timer += 0.1f;
            }

            // After strobe, fade out to dim state
            float fadeDuration = _ringDelays[0]; // Or use a dedicated strobe fade duration
            float finalIntensity = _dimIntensity * _baluminaria.maxIntensity;
            if (_currentOutputMode == OutputMode.UI)
            {
                StartFadeUI(_uiSegments[segIndex], _uiSegments[segIndex].color, GetInitialDimColor(segIndex), fadeDuration);
            }
            else
            {
                StartFadeBaluminaria(_baluSegments[segIndex], _baluSegments[segIndex]?.GetComponentInChildren<Light>().intensity ?? 0f, finalIntensity, fadeDuration);
            }
            _activeCoroutines[segIndex] = null; // Mark coroutine as finished
        }
        _activeCoroutines[segIndex] = StartCoroutine(strobeCoroutine());
    }
    #endregion

    #region Helper Methods (Calculations & Color Retrieval)

    // Optimization: Pre-calculating these could be beneficial if GetScaleDegreeIndex is called extremely often
    // private int[] _relativePitchClassToDegreeIndexMap; // Could be built in Awake
    // ...
    // private void InitializeScaleDegreeMaps() { ... }

    /// <summary>
    /// Gets the scale degree index (0-6) for the played note within the current scale.
    /// </summary>
    private int GetScaleDegreeIndex(int noteInOctave)
    {
        int tonicPitchClass = _tonalKeyNoteValue;
        int playedPitchClass = noteInOctave;
        int relativePitchClass = (playedPitchClass - tonicPitchClass + 12) % 12; // Transpose to C-based relative

        // Map the relative chromatic pitch class to its diatonic degree index (0-6)
        return _semitoneDistanceToDegreeIndex[relativePitchClass];
    }

    /// <summary>
    /// Gets the two segment indices (e.g., 0-6 and 8-14) for the played note's degree in a specific ring.
    /// </summary>
    private (int segIndexA, int segIndexB) GetNoteSegmentIndices(int noteInOctave, int ring)
    {
        int degreeIndex = GetScaleDegreeIndex(noteInOctave);
        int ringOffset = ring * SEGMENTS_PER_RING;
        return (ringOffset + degreeIndex, ringOffset + degreeIndex + 8);
    }

    /// <summary>
    /// Gets the base color for tonal modes, considering ColorMode.Note and ColorMode.Ring.
    /// </summary>
    private Color GetTonalModeBaseColor(int noteInOctave, int ring)
    {
        switch (_currentColorMode)
        {
            case ColorMode.Note:
            case ColorMode.TonalKeyPulse:
            case ColorMode.AcrossOctaves:
            case ColorMode.Random: // Random mode still uses note color for base
            case ColorMode.Strobe:
            case ColorMode.TwoPaths:
                return _noteColors[noteInOctave];

            case ColorMode.Ring:
            case ColorMode.VerticalColor:
            case ColorMode.OctavesBattle:
                return _ringColors[ring];

            default:
                return Color.white;
        }
    }

    /// <summary>
    /// Gets the color for the separator pulse based on the _separatorColorSource setting.
    /// </summary>
    private Color GetSeparatorPulseColor(int noteInOctave, int ring)
    {
        switch (_separatorColorSource)
        {
            case SeparatorColorSource.SelectedColor:
                return _separatorPulseColor;
            case SeparatorColorSource.KeyColor:
                return _noteColors[noteInOctave];
            case SeparatorColorSource.RingColor:
                return _ringColors[ring];
            default:
                return _separatorPulseColor; // Fallback
        }
    }

    /// <summary>
    /// Determines the initial dimmed color for a segment based on its position and current ColorMode.
    /// </summary>
    private Color GetInitialDimColor(int segIndex)
    {
        int ring = segIndex / SEGMENTS_PER_RING;
        int segInRing = segIndex % SEGMENTS_PER_RING;

        // 1. Separator Logic: Apply special dim color ONLY in tonal modes where separators might pulse
        if ((segInRing == FIRST_SEPARATOR_INDEX || segInRing == SECOND_SEPARATOR_INDEX) && _isTonalMappingMode)
        {
            // For dimming, use the tonic's note in octave for key color, or the current ring's color
            int noteInOctaveForColor = _tonalKeyNoteValue;
            Color basePulseColor = GetSeparatorPulseColor(noteInOctaveForColor, ring);
            return basePulseColor * _separatorDimIntensity;
        }

        Color initialColor;

        // 2. Logic for Tonal/Hybrid Modes (for non-separator segments, or separators in non-tonal modes)
        if (_isTonalMappingMode || _isTwoPathsMode)
        {
            // When dimming in tonal modes, typically use the tonic's color for the 'off' state
            int noteInOctaveForColor = _tonalKeyNoteValue;

            Color baseColor;
            // Use ring color for Ring, VerticalColor, TwoPaths (for segments not tied to specific note degree)
            if (_currentColorMode == ColorMode.Ring ||
                _currentColorMode == ColorMode.VerticalColor ||
                _currentColorMode == ColorMode.TwoPaths)
            {
                baseColor = _ringColors[ring];
            }
            else // Default to tonic note color for other tonal modes
            {
                baseColor = _noteColors[noteInOctaveForColor];
            }
            initialColor = baseColor * _dimIntensity;
        }
        // 3. Logic for Non-Tonal Modes
        else
        {
            switch (_currentColorMode)
            {
                case ColorMode.FullRing:
                    initialColor = _ringColors[ring] * _dimIntensity;
                    break;
                case ColorMode.FullBoard:
                    // For FullBoard, use a consistent dim color across the board or a default.
                    // If you want it to reflect the tonic of the current tonal setup, use _noteColors[_tonalKeyNoteValue]
                    initialColor = _noteColors[_tonalKeyNoteValue] * _dimIntensity;
                    break;
                case ColorMode.Random:
                    // In random mode, when dimmed, it falls back to a default (e.g., tonic or black)
                    initialColor = _noteColors[_tonalKeyNoteValue] * _dimIntensity;
                    break;
                default:
                    initialColor = Color.black * _dimIntensity;
                    break;
            }
        }
        return initialColor;
    }
    #endregion

    #region Configuration Setters (Public API)

    /// <summary>
    /// Handles a list of MPTKEvents (typically from MidiFilePlayer).
    /// </summary>
    private void OnNotesMidiFromFile(List<MPTKEvent> noteEvents)
    {
        // Use a for loop instead of foreach to potentially avoid enumerator allocation
        for (int i = 0; i < noteEvents.Count; i++)
        {
            HandleNoteEvent(noteEvents[i]);
        }
    }

    /// <summary>
    /// Sets the current playback mode and configures MPTK components.
    /// </summary>
    public void SetPlaybackMode(PlaybackMode mode)
    {
        // Unsubscribe from old mode's events
        if (_midiFilePlayer != null) _midiFilePlayer.OnEventNotesMidi.RemoveListener(OnNotesMidiFromFile);

        // Deactivate all related game objects
        _midiFilePlayer?.gameObject.SetActive(false);
        _midiStreamPlayer?.gameObject.SetActive(false);
        _baluAudioReactive?.gameObject.SetActive(false);

        _currentPlaybackMode = mode;

        // Activate and configure for the new mode
        switch (mode)
        {
            case PlaybackMode.File:
                if (_midiFilePlayer != null)
                {
                    _midiFilePlayer.gameObject.SetActive(true);
                    _midiFilePlayer.OnEventNotesMidi.AddListener(OnNotesMidiFromFile);
                }
                break;
            case PlaybackMode.Stream:
                _midiStreamPlayer?.gameObject.SetActive(true);
                break;
            case PlaybackMode.AudioSource:
                _baluAudioReactive?.gameObject.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// Sets the MIDI instrument for playback.
    /// </summary>
    public void SetInstrument(int programNumber, int channel)
    {
        _currentInstrumentProgram = programNumber;
        _midiChannel = channel;
        StartCoroutine(DelaySetInstrument());
    }

    private IEnumerator DelaySetInstrument()
    {
        yield return new WaitForSeconds(1f); // MPTK might need a moment to initialize fully
        // MidiPlayerGlobal.MPTK_SelectBankInstrument(_currentInstrumentProgram);
        // Note: MPTK_SelectBankInstrument sets for all channels or a specific channel if provided.
        // For per-channel instrument changes, MPTKEvent with Command=PatchChange is used.
        // If this is for global default, then the line below is fine.
        MidiPlayerGlobal.MPTK_SelectBankInstrument(_currentInstrumentProgram);
    }

    // This method is likely for editor-time changes
    public void ChangeInstrumentInEditor(int programNumber)
    {
        SetInstrument(programNumber, _midiChannel);
    }

    /// <summary>
    /// Sets the output mode (UI visualization or Baluminaria).
    /// </summary>
    public void SetOutputMode(OutputMode mode)
    {
        _currentOutputMode = mode;
        _uiCanvas?.SetActive(mode == OutputMode.UI);

        // Clear separator indices (no longer stored as a list, derived on-the-fly)

        if (_currentOutputMode == OutputMode.Baluminaria)
        {
            if (_baluminaria == null)
            {
                Debug.LogError("Baluminaria reference is null when trying to set output mode to Baluminaria.");
                return;
            }

            Segment[] allSegments = _baluminaria.GetSegments();
            if (allSegments == null || allSegments.Length != TOTAL_SEGMENTS)
            {
                Debug.LogError($"BaluMidiController expects {TOTAL_SEGMENTS} segments, but Baluminaria provided {allSegments?.Length ?? 0}. Mapping might be incorrect.");
                return;
            }

            // Optimized Baluminaria segment mapping
            for (int ring = 0; ring < 7; ring++)
            {
                for (int segInRing = 0; segInRing < SEGMENTS_PER_RING; segInRing++)
                {
                    int baluminariaSourceIndex = (segInRing * 7) + ring; // This is the physical layout mapping
                    int baluControllerTargetIndex = (ring * SEGMENTS_PER_RING) + segInRing; // This is how BaluMidiController internally views it

                    if (baluminariaSourceIndex < allSegments.Length && baluControllerTargetIndex < TOTAL_SEGMENTS)
                    {
                        _baluSegments[baluControllerTargetIndex] = allSegments[baluminariaSourceIndex];
                    }
                }
            }
        }
        else // OutputMode.UI
        {
            if (_ringsParent == null)
            {
                Debug.LogError("RingsParent reference is null for UI output mode.");
                return;
            }

            // Clear previous UI segments
            foreach (Transform child in _ringsParent)
            {
                Destroy(child.gameObject);
            }

            // Create new UI segments
            int segIndex = 0;
            for (int ring = 0; ring < 7; ring++)
            {
                for (int segInRing = 0; segInRing < SEGMENTS_PER_RING; segInRing++)
                {
                    string segName;
                    if (segInRing < FIRST_SEPARATOR_INDEX) segName = $"Grau_{segInRing + 1}_A_Anel{ring}";
                    else if (segInRing == FIRST_SEPARATOR_INDEX) segName = $"Separador_A_Anel{ring}";
                    else if (segInRing < SECOND_SEPARATOR_INDEX) segName = $"Grau_{(segInRing - 8) + 1}_B_Anel{ring}";
                    else segName = $"Separador_B_Anel{ring}";

                    Color initialColor = GetInitialDimColor(segIndex); // Get initial dim color for UI

                    GameObject segGO = new GameObject(segName, typeof(RectTransform), typeof(Image));
                    segGO.transform.SetParent(_ringsParent, false);
                    Image img = segGO.GetComponent<Image>();
                    img.sprite = _segSprite; // _segSprite can be null, handled by Image component default
                    img.type = Image.Type.Filled;
                    img.fillMethod = Image.FillMethod.Radial360;
                    img.rectTransform.sizeDelta = new Vector2(32, 32);
                    img.color = initialColor;
                    _uiSegments[segIndex] = img;

                    segIndex++;
                }
            }
        }
    }

    /// <summary>
    /// Changes the current color mode and updates internal flags.
    /// </summary>
    public void SetColorMode(ColorMode newMode)
    {
        _currentColorMode = newMode;
        UpdateColorModeFlags(); // Re-calculate flags when mode changes
    }


    public float GetSensitivity() => _analysisSensitivity;
    public float GetInterval() => _analysisInterval;
    #endregion

    #region Editor Utilities
    [ContextMenu("Restart Song")]
    public void RestartSong()
    {
        if (_currentPlaybackMode == PlaybackMode.File && _midiFilePlayer != null)
        {
            _midiFilePlayer.MPTK_Stop();
            _midiFilePlayer.MPTK_Play();
        }
        else if (_currentPlaybackMode == PlaybackMode.AudioSource && _baluAudioReactive != null)
        {
            _baluAudioReactive.RestartAudioClip();
        }
    }
    #endregion
}