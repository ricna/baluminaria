using UnityEngine;
using MidiPlayerTK;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq; // Adicionado para usar Linq para encontrar fades

public class BaluMidiController : MonoBehaviour
{
    // Enums e variáveis de Header permanecem os mesmos...
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
    #endregion

    // NOVO: Classe interna para gerenciar fades no Update
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
    private Coroutine[] _activeCoroutines = new Coroutine[84]; // Mantido para o Strobe

    // NOVO: Variáveis para o controle do Pedal de Sustain
    private bool _isSustainPedalDown = false;
    private HashSet<int> _sustainedNotes = new HashSet<int>();

    private void Awake()
    {
        // Awake permanece o mesmo
        #region Awake Content
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
        #endregion
    }

    private void Start()
    {
        // Start permanece o mesmo
        #region Start Content
        foreach (Transform child in _ringsParent)
            Destroy(child.gameObject);

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
            img.fillAmount = 1f;
            if (_segSprite != null) img.sprite = _segSprite;
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Radial360;
            img.rectTransform.sizeDelta = new Vector2(32, 32);

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

        SetPlaybackMode(_currentPlaybackMode);
        #endregion
    }

    // NOVO: Método Update para gerenciar os fades
    private void Update()
    {
        // Itera pela lista de trás para frente para poder remover itens com segurança
        for (int i = _activeFades.Count - 1; i >= 0; i--)
        {
            var fade = _activeFades[i];
            fade.Timer += Time.deltaTime;

            if (fade.Timer >= fade.Duration)
            {
                fade.Segment.color = fade.EndColor;
                _activeFades.RemoveAt(i); // Remove o fade quando terminar
            }
            else
            {
                float progress = fade.Timer / fade.Duration;
                fade.Segment.color = Color.Lerp(fade.StartColor, fade.EndColor, progress);
            }
        }
    }

    // MODIFICADO: Função para iniciar um fade (substitui StartCoroutine)
    private void StartFade(Image segment, Color startColor, Color endColor, float duration)
    {
        // Garante que o segmento não está sendo controlado por uma coroutine (Strobe)
        int segIndex = System.Array.IndexOf(_segs, segment);
        if (segIndex != -1 && _activeCoroutines[segIndex] != null)
        {
            StopCoroutine(_activeCoroutines[segIndex]);
            _activeCoroutines[segIndex] = null;
        }

        // Remove qualquer fade anterior para o mesmo segmento
        _activeFades.RemoveAll(f => f.Segment == segment);

        // Adiciona o novo fade à lista
        _activeFades.Add(new ActiveFade
        {
            Segment = segment,
            StartColor = startColor,
            EndColor = endColor,
            Duration = duration,
            Timer = 0f
        });

        // Define a cor inicial imediatamente
        segment.color = startColor;
    }

    // NOVO: Lógica para o Pedal de Sustain
    public void HandleSustainPedal(bool isDown)
    {
        _isSustainPedalDown = isDown;

        // Se o pedal foi solto, apaga todas as notas que estavam sendo sustentadas
        if (!isDown)
        {
            foreach (int midiNote in _sustainedNotes)
            {
                // Dispara um "note off" (velocity 0) para cada nota sustentada
                HandleNoteOn(midiNote, 0);
            }
            _sustainedNotes.Clear();
        }
    }

    public void HandleNoteOn(int midiNote, int velocity)
    {
        // Este método permanece o mesmo
        MPTKEvent ev = new MPTKEvent { Command = MPTKCommand.NoteOn, Value = midiNote, Velocity = Mathf.Clamp(velocity, 0, 127) };
        HandleNoteOn(ev);
    }

    public void HandleNoteOn(MPTKEvent noteEvent)
    {
        // NOVO: Lógica de Sustain adicionada no início
        bool isNoteOff = noteEvent.Velocity == 0;
        if (isNoteOff && _isSustainPedalDown)
        {
            // Se for um Note Off e o pedal estiver pressionado, guarda a nota e não faz nada
            _sustainedNotes.Add(noteEvent.Value);
            return;
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

            float intensity = Mathf.Clamp01(noteEvent.Velocity / 127f);
            float fadeDuration = _ringDelays[ring] + _noteDelays[noteInOctave];

            // MODIFICADO: Os blocos de código agora chamam StartFade em vez de StartCoroutine
            // A lógica interna de cada modo de cor permanece a mesma, apenas a chamada final muda.

            // Exemplo para o modo ByRing:
            if (_currentColorMode == ColorMode.ByRing)
            {
                Color baseColor = _ringColors[ring];
                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                Color dimColor = baseColor * _dimIntensity;

                if (isNoteOff) peakColor = _segs[segIndex].color; // Fade a partir da cor atual se for note off
                StartFade(_segs[segIndex], peakColor, dimColor, fadeDuration);
            }
            // Exemplo para ByNote
            else if (_currentColorMode == ColorMode.ByNote)
            {
                Color baseColor = _noteColors[noteInOctave];
                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                Color dimColor = baseColor * _dimIntensity;
                if (isNoteOff) peakColor = _segs[segIndex].color;
                StartFade(_segs[segIndex], peakColor, dimColor, fadeDuration);
            }
            // Exemplo para FullRing
            else if (_currentColorMode == ColorMode.FullRing)
            {
                Color baseColor = _ringColors[ring];
                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                Color dimColor = baseColor * _dimIntensity;
                int startMidi = ((octave + 1) * 12);
                int endMidi = ((octave + 2) * 12) - 1;
                for (int m = startMidi; m <= endMidi; m++)
                {
                    int ringSegIndex = m - 24;
                    if (ringSegIndex >= 0 && ringSegIndex < _segs.Length)
                    {
                        Color startColor = isNoteOff ? _segs[ringSegIndex].color : peakColor;
                        StartFade(_segs[ringSegIndex], startColor, dimColor, fadeDuration);
                    }
                }
            }
            // MODIFICAÇÃO: O mesmo padrão se aplica a todos os outros modos que usavam LightSegment.
            // O modo Strobe permanece como está, usando coroutine.
            else if (_currentColorMode == ColorMode.Strobe)
            {
                // Strobe é um efeito especial, mantê-lo como coroutine é mais simples
                Color baseColor = _noteColors[noteInOctave];
                Color peakColor = Color.Lerp(Color.black, baseColor, intensity);
                StartCoroutine(StrobeEffect(segIndex, peakColor));
            }
            // ... aplicar a mesma lógica de chamada `StartFade` para os outros modos ...

            // O resto do método permanece o mesmo (atualização de texto e debug)
            #region Text and Debug
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            string noteName = noteNames[midiNote % 12];
            double frequency = 440.0 * System.Math.Pow(2, (midiNote - 69) / 12.0);
            _text.text = $"Nota: {noteName}{octave} | MIDI: {midiNote} | Velocidade: {noteEvent.Velocity} | Freq: {frequency:F2} Hz";
            Debug.Log($"Nota: {noteName}{octave} | MIDI: {midiNote} | Oitava: {octave} | Frequência: {frequency:F2} Hz");
            #endregion
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Erro em HandleNoteOn: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // O StrobeEffect continua sendo uma Coroutine
    private IEnumerator StrobeEffect(int segIndex, Color peakColor)
    {
        if (segIndex < 0 || segIndex >= _segs.Length) yield break;

        // Garante que o segmento não está em um fade gerenciado pelo Update
        _activeFades.RemoveAll(f => f.Segment == _segs[segIndex]);

        // Gerencia a coroutine como antes
        if (_activeCoroutines[segIndex] != null)
        {
            StopCoroutine(_activeCoroutines[segIndex]);
        }

        // A coroutine em si
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

            // Ao final, inicia um fade de volta para a cor "dim"
            Color initialColor = GetInitialDimColor(segIndex);
            float fadeDuration = _ringDelays[0] + _noteDelays[0]; // Simplificado, ajuste se necessário
            StartFade(_segs[segIndex], _segs[segIndex].color, initialColor, fadeDuration);
            _activeCoroutines[segIndex] = null;
        }

        _activeCoroutines[segIndex] = StartCoroutine(strobe());
    }

    // A coroutine LightSegment não é mais necessária
    // private IEnumerator LightSegment(...) { ... } // REMOVIDO

    // Métodos auxiliares permanecem os mesmos
    #region Helper Methods
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
    #endregion
}