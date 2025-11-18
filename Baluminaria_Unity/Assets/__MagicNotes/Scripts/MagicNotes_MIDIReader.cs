using UnityEngine;
using MidiJack;

public class MagicNotes_MIDIReader : MonoBehaviour
{
    public delegate void NoteOnEvent(int note, int velocity);
    public delegate void NoteOffEvent(int note);

    public static event NoteOnEvent OnNoteOn;
    public static event NoteOffEvent OnNoteOff;

    private void OnEnable()
    {
        MidiMaster.noteOnDelegate += HandleNoteOn;
        MidiMaster.noteOffDelegate += HandleNoteOff;
    }

    private void OnDisable()
    {
        MidiMaster.noteOnDelegate -= HandleNoteOn;
        MidiMaster.noteOffDelegate -= HandleNoteOff;
    }

    private void HandleNoteOn(MidiChannel channel, int note, float velocity)
    {
        int velInt = Mathf.Clamp(Mathf.RoundToInt(velocity * 127f), 1, 127);
        OnNoteOn?.Invoke(note, velInt);
    }

    private void HandleNoteOff(MidiChannel channel, int note)
    {
        OnNoteOff?.Invoke(note);
    }
}
