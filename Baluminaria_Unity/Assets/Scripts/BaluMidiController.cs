using MidiPlayerTK;
using MPTK.NAudio.Midi;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaluMidiController : MonoBehaviour
{
    // Enum to control playback mode
    public enum PlaybackMode
    {
        File,
        Stream,
        AudioSource
    }

    // Enum to control color mode
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

    // NEW: Enum to control output mode
    public enum OutputMode
    {
        UI,
        Baluminaria
    }

    [Header("MPTK Components")]
    [SerializeField] private MidiFilePlayer _midiFilePlayer;
    [SerializeField] private MidiStreamPlayer _midiStreamPlayer;
    [SerializeField] private BaluAudioReactive _baluAudioReactive;
    [SerializeField] private AudioSource _audioSource;

    [Header("Information Display")]
    [SerializeField] private TMP_Text _text;
    [SerializeField] private Transform _ringsParent;

    // NEW: Output Settings
    [Header("Output Settings")]
    [SerializeField] private OutputMode _currentOutputMode = OutputMode.UI;
    [SerializeField] private GameObject _uiCanvas;
    [SerializeField] private Baluminaria _baluminaria;

    [Header("Playback Settings")]
    [SerializeField] private PlaybackMode _currentPlaybackMode = PlaybackMode.File;
    [SerializeField] private bool _clampNotes = true;

    [Header("Analysis Settings")]
    [SerializeField]
    [Range(0.1f, 0.01f)]
    private float _analysisSensitivity = 0.03f;
    [SerializeField]
    [Range(0.01f, 0.5f)]
    private float _analysisInterval = 0.01f; // Faster analysis interval

    [Header("Color Mode")]
    [SerializeField] private ColorMode _currentColorMode = ColorMode.ByRing;

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

    [SerializeField] private Sprite _segSprite;

    [Header("Visual Settings")]
    [SerializeField] private float _dimIntensity = 0.1f;
    [SerializeField] private float _pulseDuration = 0.1f;
    [SerializeField] private float _strobeDuration = 0.2f;

    [SerializeField]
    private float _extraFadeOutTime = 2f; // Extra time to smooth the fade-out
    [Header("Delays")]
    [SerializeField] private float[] _ringDelays = new float[7];
    [SerializeField] private float[] _noteDelays = new float[12];

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

    private Image[] _uiSegments = new Image[84];
    private Segment[] _baluSegments = new Segment[84];

    private Coroutine[] _activeCoroutines = new Coroutine[84];

    private bool _isSustainPedalDown = false;
    private HashSet<int> _sustainedNotes = new HashSet<int>();

    private void Awake()
    {
        _noteColors[0] = new Color(1f, 0.0f, 0.0f);
        _noteColors[2] = new Color(1f, 0.5f, 0.0f);
        _noteColors[4] = new Color(1f, 1.0f, 0.0f);
        _noteColors[5] = new Color(0.5f, 1.0f, 0.0f);
        _noteColors[7] = new Color(0.0f, 1.0f, 0.0f);
        _noteColors[9] = new Color(0.0f, 0.5f, 1.0f);
        _noteColors[11] = new Color(0.5f, 0.0f, 1.0f);
        _noteColors[1] = Color.Lerp(_noteColors[0], _noteColors[2], 0.5f);
        _noteColors[3] = Color.Lerp(_noteColors[2], _noteColors[4], 0.5f);
        _noteColors[6] = Color.Lerp(_noteColors[5], _noteColors[7], 0.5f);
        _noteColors[8] = Color.Lerp(_noteColors[7], _noteColors[9], 0.5f);
        _noteColors[10] = Color.Lerp(_noteColors[9], _noteColors[11], 0.5f);

        if (_ringDelays.Length < 7) _ringDelays = new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
        if (_noteDelays.Length < 12) _noteDelays = new float[] { 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f };
    }

    private IEnumerator Start()
    {
        SetOutputMode(_currentOutputMode);
        yield return new WaitForSeconds(0.2f);
        SetPlaybackMode(_currentPlaybackMode);
    }

    private void Update()
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

        _activeFadesBaluminaria.RemoveAll(f => f.Segment == segment);

        /*if (endIntensity > 0 && _currentPlaybackMode == PlaybackMode.File)
        {
            //Fade imediato para evitar delay no ataque
            segment.SetIntensity(endIntensity);
            return;
        }*/

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

    public void HandleNoteOn(int midiNote, int velocity)
    {
        MPTKEvent ev = new MPTKEvent { Command = MPTKCommand.NoteOn, Value = midiNote, Velocity = Mathf.Clamp(velocity, 0, 127) };
        HandleNoteEvent(ev);
    }
    public void HandleNoteEvent(MPTKEvent noteEvent)
    {
        bool isNoteOffEvent = noteEvent.Velocity == 0; // Se o evento original vindo do MidiJack é um Note Off.

        // Lógica de sustain: Adiciona a nota à lista de sustentadas SE o evento original é Note Off E o pedal está pressionado.
        if (isNoteOffEvent && _isSustainPedalDown)
        {
            _sustainedNotes.Add(noteEvent.Value);
            // IMPORTANTE: Não enviamos o Note Off para o MidiStreamPlayer AINDA,
            // pois o pedal está segurando a nota. Saímos daqui para não processar
            // o Note Off para o som, mas a visualização ainda pode ser atualizada
            // se o FadeOut for inteligente sobre o sustain.
            // Para a visualização, o FadeOut é chamado abaixo, e ele já tem a lógica do _isSustainPedalDown.
        }


        try
        {
            int midiNote = noteEvent.Value;
            if (_clampNotes)
            {
                midiNote = Mathf.Clamp(midiNote, 24, 107);
            }
            else if (midiNote < 24 || midiNote > 107) return;

            int segIndex = midiNote - 24;
            int octave = (midiNote / 12) - 1;
            int noteInOctave = midiNote % 12;
            int ring = Mathf.Clamp(octave - 1, 0, _ringColors.Length - 1);

            // --- BLOCO PARA O SOM (AGORA COM SUSTAIN) ---
            if (_currentPlaybackMode == PlaybackMode.Stream && _midiStreamPlayer != null)
            {
                if (noteEvent.Velocity > 0) // É um evento Note On (nota sendo pressionada)
                {
                    // Diz ao MidiStreamPlayer para tocar a nota.
                    // Se esta nota estava sustentada por um pedal, ela não está mais.
                    _sustainedNotes.Remove(noteEvent.Value);
                    _midiStreamPlayer.MPTK_PlayEvent(noteEvent);
                    Debug.Log($"<color=green>StreamPlayer: Tocou Nota {noteEvent.Value} com Vel. {noteEvent.Velocity}</color>");
                }
                else // É um evento Note Off (nota sendo liberada pelo teclado)
                {
                    // VERIFICAR SUSTAIN ANTES DE ENVIAR O NOTE OFF AO MidiStreamPlayer
                    if (!_isSustainPedalDown || !_sustainedNotes.Contains(noteEvent.Value))
                    {
                        // Se o pedal NÃO está pressionado OU a nota NÃO está na lista de sustentadas,
                        // então podemos enviar o Note Off para o MidiStreamPlayer.
                        _midiStreamPlayer.MPTK_PlayEvent(noteEvent); // noteEvent.Velocity já é 0 aqui.
                        _sustainedNotes.Remove(noteEvent.Value); // Remove da lista de sustentadas, pois a nota realmente parou.
                        Debug.Log($"<color=red>StreamPlayer: Parou Nota {noteEvent.Value}</color>");
                    }
                    else
                    {
                        // A nota é um Note Off do teclado, mas está sendo sustentada pelo pedal.
                        // Não enviamos o Note Off para o MidiStreamPlayer.
                        Debug.Log($"<color=orange>StreamPlayer: Nota {noteEvent.Value} sustentada pelo pedal, não enviou Note Off.</color>");
                    }
                }
            }
            // --- FIM DO BLOCO PARA O SOM ---

            // --- Lógica da Visualização (já lida com sustain através do fadeDuration) ---
            if (isNoteOffEvent) // Se o evento original era um Note Off (do MidiJack)
            {
                // A visualização deve sempre ter um FadeOut, mas sua duração depende do sustain.
                // Note: O FadeOut da visualização pode ser prolongado pelo sustain,
                // mesmo que o som continue se o pedal estiver pressionado. Isso é um design
                // a ser considerado. Se você quer que a visualização fique 'acesa' enquanto
                // o som é sustentado, talvez o FadeOut só devesse ocorrer quando o som realmente parar.
                // Mas a lógica atual já é mais robusta que antes.
                float fadeDuration = _ringDelays[ring] + _noteDelays[noteInOctave];
                FadeOut(midiNote, segIndex, ring, noteInOctave, fadeDuration);
            }
            else // É um Note On
            {
                float velocityIntensity = Mathf.Clamp01(noteEvent.Velocity / 127f);
                LightUp(midiNote, segIndex, ring, noteInOctave, velocityIntensity);
                if (_currentPlaybackMode == PlaybackMode.File)
                {
                    float fadeDuration = _ringDelays[ring] + _noteDelays[noteInOctave];
                    FadeOut(midiNote, segIndex, ring, noteInOctave, fadeDuration);
                }
            }

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


    // Método que é chamado quando o pedal de sustain muda de estado
    // Este método é chamado do BaluMidiAdapter
    public void HandleSustainPedal(bool isDown)
    {
        _isSustainPedalDown = isDown;
        if (!isDown) // Se o pedal foi liberado
        {
            Debug.Log("<color=purple>Pedal de Sustain LIBERADO. Parando notas sustentadas.</color>");
            foreach (int midiNote in _sustainedNotes)
            {
                // Criamos um Note Off manual para cada nota que estava sendo sustentada
                MPTKEvent noteOffEvent = new MPTKEvent { Command = MPTKCommand.NoteOn, Value = midiNote, Velocity = 0 };
                if (_currentPlaybackMode == PlaybackMode.Stream && _midiStreamPlayer != null)
                {
                    _midiStreamPlayer.MPTK_PlayEvent(noteOffEvent); // Envia o Note Off para o MidiStreamPlayer
                }
                // Também chamamos HandleNoteEvent para que a visualização reaja ao fim do sustain
                // (isso fará o FadeOut final, se ele ainda não tiver ocorrido).
                // Precisamos ter cuidado para não entrar em loop infinito aqui se HandleNoteEvent chamar HandleSustainPedal.
                // No seu caso, HandleNoteEvent NÃO chama HandleSustainPedal, então é seguro.
                HandleNoteEvent(noteOffEvent); // Atualiza a visualização para o Note Off final
            }
            _sustainedNotes.Clear(); // Limpa todas as notas sustentadas
        }
        else
        {
            Debug.Log("<color=purple>Pedal de Sustain PRESSIONADO.</color>");
        }
    }

    private void LightUp(int midiNote, int segIndex, int ring, int noteInOctave, float intensity)
    {
        Color baseColor;
        float targetIntensity = intensity * _baluminaria.maxIntensity;

        switch (_currentColorMode)
        {
            case ColorMode.ByRing:
                baseColor = _ringColors[ring];
                if (_currentOutputMode == OutputMode.UI)
                {
                    _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[segIndex]);
                    _uiSegments[segIndex].color = Color.Lerp(Color.black, baseColor, intensity);
                }
                else
                {
                    _baluSegments[segIndex].ChangeLightColor(baseColor);
                    StartFadeBaluminaria(_baluSegments[segIndex], 0f, targetIntensity);
                }
                break;

            case ColorMode.ByNote:
                baseColor = _noteColors[noteInOctave];
                if (_currentOutputMode == OutputMode.UI)
                {
                    _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[segIndex]);
                    _uiSegments[segIndex].color = Color.Lerp(Color.black, baseColor, intensity);
                }
                else
                {
                    _baluSegments[segIndex].ChangeLightColor(baseColor);
                    StartFadeBaluminaria(_baluSegments[segIndex], 0f, targetIntensity);
                }
                break;
            case ColorMode.AcrossOctaves:
                baseColor = _noteColors[noteInOctave];
                if (_currentOutputMode == OutputMode.UI)
                {
                    for (int i = 0; i < _uiSegments.Length; i++)
                    {
                        int currentNoteInOctave = (i + 24) % 12;
                        if (currentNoteInOctave == noteInOctave)
                        {
                            _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[i]);
                            _uiSegments[i].color = Color.Lerp(Color.black, baseColor, intensity);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _baluSegments.Length; i++)
                    {
                        int currentNoteInOctave = (i + 24) % 12;
                        if (currentNoteInOctave == noteInOctave)
                        {
                            _baluSegments[i].ChangeLightColor(baseColor);
                            StartFadeBaluminaria(_baluSegments[i], 0f, targetIntensity);
                        }
                    }
                }
                break;
            case ColorMode.VerticalColor:
                baseColor = _ringColors[ring];
                if (_currentOutputMode == OutputMode.UI)
                {
                    for (int i = 0; i < _uiSegments.Length; i++)
                    {
                        int currentNoteInOctave = (i + 24) % 12;
                        if (currentNoteInOctave == noteInOctave)
                        {
                            _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[i]);
                            _uiSegments[i].color = Color.Lerp(Color.black, baseColor, intensity);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < _baluSegments.Length; i++)
                    {
                        int currentNoteInOctave = (i + 24) % 12;
                        if (currentNoteInOctave == noteInOctave)
                        {
                            _baluSegments[i].ChangeLightColor(baseColor);
                            StartFadeBaluminaria(_baluSegments[i], 0f, targetIntensity);
                        }
                    }
                }
                break;

            case ColorMode.TwoPaths:
            case ColorMode.OctavesBattle:
                baseColor = (_currentColorMode == ColorMode.TwoPaths) ? _noteColors[noteInOctave] : _noteColors[ring];
                // Acende a linha vertical
                for (int i = 0; i < _uiSegments.Length; i++)
                {
                    int currentNoteInOctave = (i + 24) % 12;
                    if (currentNoteInOctave == noteInOctave)
                    {
                        if (_currentOutputMode == OutputMode.UI)
                        {
                            _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[i]);
                            _uiSegments[i].color = Color.Lerp(Color.black, baseColor, intensity);
                        }
                        else
                        {
                            _baluSegments[i].ChangeLightColor(baseColor);
                            StartFadeBaluminaria(_baluSegments[i], 0f, targetIntensity);
                        }
                    }
                }
                // Acende o anel horizontal
                int startMidi = ((ring + 2) * 12);
                int endMidi = startMidi + 11;
                for (int m = startMidi; m <= endMidi; m++)
                {
                    int currentSegIndex = m - 24;
                    if (currentSegIndex >= 0 && currentSegIndex < _uiSegments.Length)
                    {
                        if (_currentOutputMode == OutputMode.UI)
                        {
                            _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[currentSegIndex]);
                            _uiSegments[currentSegIndex].color = Color.Lerp(Color.black, baseColor, intensity);
                        }
                        else
                        {
                            _baluSegments[currentSegIndex].ChangeLightColor(baseColor);
                            StartFadeBaluminaria(_baluSegments[currentSegIndex], 0f, targetIntensity);
                        }
                    }
                }
                break;
            case ColorMode.FullRing:
                baseColor = _ringColors[ring];
                int startMidiFullRing = ((ring + 2) * 12);
                int endMidiFullRing = startMidiFullRing + 11;
                for (int m = startMidiFullRing; m <= endMidiFullRing; m++)
                {
                    int currentSegIndex = m - 24;
                    if (currentSegIndex >= 0 && currentSegIndex < _uiSegments.Length)
                    {
                        if (_currentOutputMode == OutputMode.UI)
                        {
                            _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[currentSegIndex]);
                            _uiSegments[currentSegIndex].color = Color.Lerp(Color.black, baseColor, intensity);
                        }
                        else
                        {
                            _baluSegments[currentSegIndex].ChangeLightColor(baseColor);
                            StartFadeBaluminaria(_baluSegments[currentSegIndex], 0f, targetIntensity);
                        }
                    }
                }
                break;
            case ColorMode.Strobe:
                baseColor = _noteColors[noteInOctave];
                if (_currentOutputMode == OutputMode.UI)
                {
                    _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[segIndex]);
                    _uiSegments[segIndex].color = Color.Lerp(Color.black, baseColor, intensity);
                }
                else
                {
                    _baluSegments[segIndex].ChangeLightColor(baseColor);
                }
                StartCoroutine(StrobeEffect(segIndex, targetIntensity));
                break;
            case ColorMode.FullBoardPulse:
                baseColor = _noteColors[noteInOctave];
                for (int i = 0; i < _uiSegments.Length; i++)
                {
                    if (_currentOutputMode == OutputMode.UI)
                    {
                        _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[i]);
                        _uiSegments[i].color = Color.Lerp(Color.black, baseColor, intensity);
                    }
                    else
                    {
                        _baluSegments[i].ChangeLightColor(baseColor);
                        StartFadeBaluminaria(_baluSegments[i], 0f, targetIntensity);
                    }
                }
                break;

            case ColorMode.RandomNoteColor:
                baseColor = new Color(Random.value, Random.value, Random.value);
                if (_currentOutputMode == OutputMode.UI)
                {
                    _activeFadesUI.RemoveAll(f => f.Segment == _uiSegments[segIndex]);
                    _uiSegments[segIndex].color = Color.Lerp(Color.black, baseColor, intensity);
                }
                else
                {
                    _baluSegments[segIndex].ChangeLightColor(baseColor);
                    StartFadeBaluminaria(_baluSegments[segIndex], 0f, targetIntensity);
                }
                break;
        }
    }

    private void FadeOut(int midiNote, int segIndex, int ring, int noteInOctave, float fadeDuration)
    {
        fadeDuration = _isSustainPedalDown ? fadeDuration + _extraFadeOutTime : fadeDuration;
        float dimIntensity = _dimIntensity * _baluminaria.maxIntensity;

        switch (_currentColorMode)
        {
            case ColorMode.ByRing:
            case ColorMode.FullRing:
                if (_currentOutputMode == OutputMode.UI)
                {
                    StartFadeUI(_uiSegments[segIndex], _uiSegments[segIndex].color, _ringColors[ring] * _dimIntensity, fadeDuration);
                }
                else
                {
                    StartFadeBaluminaria(_baluSegments[segIndex], _baluSegments[segIndex].GetComponentInChildren<Light>().intensity, dimIntensity, fadeDuration);
                }
                if (_currentColorMode == ColorMode.FullRing)
                {
                    int startMidiFullRing = ((ring + 2) * 12);
                    int endMidiFullRing = startMidiFullRing + 11;
                    for (int m = startMidiFullRing; m <= endMidiFullRing; m++)
                    {
                        int currentSegIndex = m - 24;
                        if (currentSegIndex >= 0 && currentSegIndex < _uiSegments.Length)
                        {
                            if (_currentOutputMode == OutputMode.UI)
                            {
                                StartFadeUI(_uiSegments[currentSegIndex], _uiSegments[currentSegIndex].color, _ringColors[ring] * _dimIntensity, fadeDuration);
                            }
                            else
                            {
                                StartFadeBaluminaria(_baluSegments[currentSegIndex], _baluSegments[currentSegIndex].GetComponentInChildren<Light>().intensity, dimIntensity, fadeDuration);
                            }
                        }
                    }
                }
                break;

            case ColorMode.ByNote:
            case ColorMode.RandomNoteColor:
                if (_currentOutputMode == OutputMode.UI)
                {
                    StartFadeUI(_uiSegments[segIndex], _uiSegments[segIndex].color, _noteColors[noteInOctave] * _dimIntensity, fadeDuration);
                }
                else
                {
                    StartFadeBaluminaria(_baluSegments[segIndex], _baluSegments[segIndex].GetComponentInChildren<Light>().intensity, dimIntensity, fadeDuration);
                }
                break;

            case ColorMode.AcrossOctaves:
                for (int i = 0; i < _uiSegments.Length; i++)
                {
                    int currentNoteInOctave = (i + 24) % 12;
                    if (currentNoteInOctave == noteInOctave)
                    {
                        if (_currentOutputMode == OutputMode.UI)
                        {
                            StartFadeUI(_uiSegments[i], _uiSegments[i].color, _noteColors[noteInOctave] * _dimIntensity, fadeDuration);
                        }
                        else
                        {
                            StartFadeBaluminaria(_baluSegments[i], _baluSegments[i].GetComponentInChildren<Light>().intensity, dimIntensity, fadeDuration);
                        }
                    }
                }
                break;
            case ColorMode.VerticalColor:
                for (int i = 0; i < _uiSegments.Length; i++)
                {
                    int currentNoteInOctave = (i + 24) % 12;
                    if (currentNoteInOctave == noteInOctave)
                    {
                        if (_currentOutputMode == OutputMode.UI)
                        {
                            StartFadeUI(_uiSegments[i], _uiSegments[i].color, _ringColors[ring] * _dimIntensity, fadeDuration);
                        }
                        else
                        {
                            StartFadeBaluminaria(_baluSegments[i], _baluSegments[i].GetComponentInChildren<Light>().intensity, dimIntensity, fadeDuration);
                        }
                    }
                }
                break;

            case ColorMode.TwoPaths:
            case ColorMode.OctavesBattle:
                Color dimColor = (_currentColorMode == ColorMode.TwoPaths) ? _noteColors[noteInOctave] * _dimIntensity : _noteColors[ring] * _dimIntensity;
                // Fade-out da linha vertical
                for (int i = 0; i < _uiSegments.Length; i++)
                {
                    int currentNoteInOctave = (i + 24) % 12;
                    if (currentNoteInOctave == noteInOctave)
                    {
                        if (_currentOutputMode == OutputMode.UI)
                        {
                            StartFadeUI(_uiSegments[i], _uiSegments[i].color, dimColor, fadeDuration);
                        }
                        else
                        {
                            StartFadeBaluminaria(_baluSegments[i], _baluSegments[i].GetComponentInChildren<Light>().intensity, dimIntensity, fadeDuration);
                        }
                    }
                }
                // Fade-out do anel horizontal
                int startMidiTwoPaths = ((ring + 2) * 12);
                int endMidiTwoPaths = startMidiTwoPaths + 11;
                for (int m = startMidiTwoPaths; m <= endMidiTwoPaths; m++)
                {
                    int currentSegIndex = m - 24;
                    if (currentSegIndex >= 0 && currentSegIndex < _uiSegments.Length)
                    {
                        if (_currentOutputMode == OutputMode.UI)
                        {
                            StartFadeUI(_uiSegments[currentSegIndex], _uiSegments[currentSegIndex].color, dimColor, fadeDuration);
                        }
                        else
                        {
                            StartFadeBaluminaria(_baluSegments[currentSegIndex], _baluSegments[currentSegIndex].GetComponentInChildren<Light>().intensity, dimIntensity, fadeDuration);
                        }
                    }
                }
                break;
            case ColorMode.FullBoardPulse:
                for (int i = 0; i < _uiSegments.Length; i++)
                {
                    Color initialDimColor = GetInitialDimColor(i);
                    if (_currentOutputMode == OutputMode.UI)
                    {
                        StartFadeUI(_uiSegments[i], _uiSegments[i].color, initialDimColor, fadeDuration);
                    }
                    else
                    {
                        StartFadeBaluminaria(_baluSegments[i], _baluSegments[i].GetComponentInChildren<Light>().intensity, dimIntensity, fadeDuration);
                    }
                }
                break;
            case ColorMode.Strobe:
                // O fade-out já é tratado dentro da corrotina de Strobe, então não faz nada aqui.
                break;
        }
    }

    private IEnumerator StrobeEffect(int segIndex, float targetIntensity)
    {
        if (segIndex < 0 || segIndex >= _uiSegments.Length) yield break;

        if (_activeCoroutines[segIndex] != null) StopCoroutine(_activeCoroutines[segIndex]);

        IEnumerator strobe()
        {
            float timer = 0f;
            while (timer < _strobeDuration)
            {
                if (_currentOutputMode == OutputMode.UI) _uiSegments[segIndex].color = _uiSegments[segIndex].color;
                else _baluSegments[segIndex].SetIntensity(targetIntensity);
                yield return new WaitForSeconds(0.05f);

                if (_currentOutputMode == OutputMode.UI) _uiSegments[segIndex].color = Color.black;
                else _baluSegments[segIndex].SetIntensity(0);
                yield return new WaitForSeconds(0.05f);
                timer += 0.1f;
            }
            float fadeDuration = _ringDelays[0];
            float finalIntensity = _dimIntensity * _baluminaria.maxIntensity;
            if (_currentOutputMode == OutputMode.UI)
            {
                StartFadeUI(_uiSegments[segIndex], _uiSegments[segIndex].color, GetInitialDimColor(segIndex), fadeDuration);
            }
            else
            {
                StartFadeBaluminaria(_baluSegments[segIndex], _baluSegments[segIndex].GetComponentInChildren<Light>().intensity, finalIntensity, fadeDuration);
            }
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

    private void OnNotesMidiFromFile(List<MPTKEvent> noteEvents)
    {
        foreach (var noteEvent in noteEvents)
        {
            HandleNoteEvent(noteEvent);
            Debug.Log($"<color=magenta>Evento FILE MIDI: {noteEvent.Command}, Nota: {noteEvent.Value}, Velocidade: {noteEvent.Velocity}, Channel: {noteEvent.Channel}</color>");
        }
    }

    private void SetPlaybackMode(PlaybackMode mode)
    {
        if (_midiFilePlayer != null) _midiFilePlayer.gameObject.SetActive(false);
        if (_midiStreamPlayer != null) _midiStreamPlayer.gameObject.SetActive(false);
        if (_baluAudioReactive != null) _baluAudioReactive.gameObject.SetActive(false);
        if (_audioSource != null) _audioSource.gameObject.SetActive(false);
        if (_midiFilePlayer != null) _midiFilePlayer.OnEventNotesMidi.RemoveListener(OnNotesMidiFromFile);

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
                if (_midiStreamPlayer != null) _midiStreamPlayer.gameObject.SetActive(true);
                break;
            case PlaybackMode.AudioSource:
                if (_baluAudioReactive != null) _baluAudioReactive.gameObject.SetActive(true);
                if (_audioSource != null) _audioSource.gameObject.SetActive(true);
                break;
        }
    }

    public void SetOutputMode(OutputMode mode)
    {
        _currentOutputMode = mode;

        if (_uiCanvas != null)
        {
            _uiCanvas.SetActive(mode == OutputMode.UI);
        }

        if (_baluminaria != null)
        {
            Segment[] allSegments = _baluminaria.GetSegments();
            int baluSegmentIndex = 0;
            for (int i = 0; i < 7; i++) // Itera sobre os 7 anéis
            {
                for (int j = 0; j < 12; j++) // Itera sobre os 12 segmentos de cada anel que serão controlados
                {
                    // O índice correto na array allSegments é j*7 + i
                    int baluminariaIndex = (j * 7) + i;
                    _baluSegments[baluSegmentIndex] = allSegments[baluminariaIndex];
                    baluSegmentIndex++;
                }
            }
        }

        if (_currentOutputMode == OutputMode.UI)
        {
            if (_ringsParent != null)
            {
                foreach (Transform child in _ringsParent) Destroy(child.gameObject);

                string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
                int segIndex = 0;
                for (int midiNote = 24; midiNote <= 107; midiNote++, segIndex++)
                {
                    int octave = (midiNote / 12) - 1;
                    int noteInOctave = midiNote % 12;
                    string segName = $"Seg{octave:00}[{noteNames[noteInOctave]}{octave}]";
                    GameObject segGO = new GameObject(segName, typeof(RectTransform), typeof(Image), typeof(Text));
                    segGO.transform.SetParent(_ringsParent, false);
                    Image img = segGO.GetComponent<Image>();
                    if (_segSprite != null) img.sprite = _segSprite;
                    img.type = Image.Type.Filled;
                    img.fillMethod = Image.FillMethod.Radial360;
                    img.rectTransform.sizeDelta = new Vector2(32, 32);
                    img.color = GetInitialDimColor(segIndex);
                    _uiSegments[segIndex] = img;
                }
            }
        }
    }

    public float GetSensitivity()
    {
        return _analysisSensitivity;
    }

    public float GetInterval()
    {
        return _analysisInterval;
    }
    #endregion

    [ContextMenu("Restart Song")]
    public void RestartSong()
    {
        if (_currentPlaybackMode == PlaybackMode.File && _midiFilePlayer != null)
        {
            _midiFilePlayer.MPTK_Stop();
            _midiFilePlayer.MPTK_Play();
        }
        else if (_currentPlaybackMode == PlaybackMode.AudioSource)
        {
            _baluAudioReactive.RestartAudioClip();
        }
    }
}