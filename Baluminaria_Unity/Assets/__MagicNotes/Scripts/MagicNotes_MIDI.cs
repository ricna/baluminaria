using System.Collections.Generic;
using UnityEngine;
using MidiPlayerTK;
using MPTK.NAudio.Midi;
using MidiJack;

public class MagicNotes_MIDI : MonoBehaviour
{
    public static MagicNotes_MIDI Instance;

    public enum PlaybackMode
    {
        File,       // Usa MidiFilePlayer (MPTK)
        Stream      // Usa MidiStreamPlayer + MidiJack
    }

    [Header("Playback Mode")]
    [SerializeField] private PlaybackMode playbackMode = PlaybackMode.Stream;

    [Header("MPTK Components")]
    [SerializeField] private MidiFilePlayer midiFilePlayer;
    [SerializeField] private MidiStreamPlayer midiStreamPlayer;

    public delegate void NoteOnEvent(int note, int velocity);
    public delegate void NoteOffEvent(int note);
    public static event NoteOnEvent OnNoteOn;
    public static event NoteOffEvent OnNoteOff;

    private bool sustainPedal = false;
    private HashSet<int> sustainedNotes = new HashSet<int>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (playbackMode == PlaybackMode.File)
        {
            midiFilePlayer.OnEventNotesMidi.AddListener(OnMidiFileEvents);
        }
        else
        {
            MidiMaster.noteOnDelegate += OnNoteOn_Live;
            MidiMaster.noteOffDelegate += OnNoteOff_Live;
            MidiMaster.knobDelegate += OnControlChange;
        }
    }

    private void OnDisable()
    {
        midiFilePlayer.OnEventNotesMidi.RemoveListener(OnMidiFileEvents);

        MidiMaster.noteOnDelegate -= OnNoteOn_Live;
        MidiMaster.noteOffDelegate -= OnNoteOff_Live;
        MidiMaster.knobDelegate -= OnControlChange;
    }

    // ----------------------------------------------------------------------
    // FILE MODE
    // ----------------------------------------------------------------------
    private void OnMidiFileEvents(List<MPTKEvent> evts)
    {
        if (playbackMode != PlaybackMode.File)
            return;

        for (int i = 0; i < evts.Count; i++)
        {
            var e = evts[i];

            if (e.Command == MPTKCommand.NoteOn)
            {
                if (e.Velocity > 0)
                    TriggerNoteOn(e.Value, e.Velocity, playSound: false);
                else
                    TriggerNoteOff(e.Value, playSound: false);
            }
        }
    }

    // ----------------------------------------------------------------------
    // LIVE MODE (MidiJack → MidiStreamPlayer)
    // ----------------------------------------------------------------------
    private void OnNoteOn_Live(MidiChannel ch, int note, float velocity)
    {
        if (playbackMode != PlaybackMode.Stream)
            return;

        int vel = Mathf.Clamp(Mathf.RoundToInt(velocity * 127f), 1, 127);

        // Toca som
        MPTKEvent noteOn = new MPTKEvent()
        {
            Command = MPTKCommand.NoteOn,
            Value = note,
            Velocity = vel
        };
        midiStreamPlayer.MPTK_PlayEvent(noteOn);

        // Evento unificado
        TriggerNoteOn(note, vel, playSound: false);
    }

    private void OnNoteOff_Live(MidiChannel ch, int note)
    {
        if (playbackMode != PlaybackMode.Stream)
            return;

        MPTKEvent noteOff = new MPTKEvent()
        {
            Command = MPTKCommand.NoteOn,
            Value = note,
            Velocity = 0
        };

        // Toca som
        midiStreamPlayer.MPTK_PlayEvent(noteOff);

        TriggerNoteOff(note, playSound: false);
    }

    // Sustain pedal (controle 64)
    private void OnControlChange(MidiChannel ch, int ctrl, float value)
    {
        if (ctrl != 64) return;

        bool down = value > 0f;
        sustainPedal = down;

        if (!down)
        {
            foreach (int note in sustainedNotes)
                TriggerNoteOff(note, playSound: false);

            sustainedNotes.Clear();
        }
    }

    // ----------------------------------------------------------------------
    // EVENTO UNIFICADO
    // ----------------------------------------------------------------------
    private void TriggerNoteOn(int note, int velocity, bool playSound)
    {
        OnNoteOn?.Invoke(note, velocity);
    }

    private void TriggerNoteOff(int note, bool playSound)
    {
        if (sustainPedal)
        {
            sustainedNotes.Add(note);
            return;
        }

        OnNoteOff?.Invoke(note);
    }

    // ----------------------------------------------------------------------
    public void SetPlaybackMode(PlaybackMode mode)
    {
        playbackMode = mode;
        OnDisable();
        OnEnable();
    }
}
