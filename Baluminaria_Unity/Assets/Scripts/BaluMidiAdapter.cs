using UnityEngine;
using MidiJack;

/// <summary>
/// Conecta entrada MIDI real (via MidiJack) ao BaluMidiController.
/// </summary>
public class BaluMidiAdapter : MonoBehaviour
{
    [SerializeField] private BaluMidiController baluController;

    private void OnEnable()
    {
        MidiMaster.noteOnDelegate += OnNoteOn;
        MidiMaster.noteOffDelegate += OnNoteOff;
        // NOVO: Inscri��o no delegate de Control Change
        MidiMaster.knobDelegate += OnControlChange;
    }

    private void OnDisable()
    {
        MidiMaster.noteOnDelegate -= OnNoteOn;
        MidiMaster.noteOffDelegate -= OnNoteOff;
        // NOVO: Remo��o da inscri��o no delegate de Control Change
        MidiMaster.knobDelegate -= OnControlChange;
    }

    private void OnNoteOn(MidiChannel channel, int note, float velocity)
    {
        int vel = Mathf.Clamp(Mathf.RoundToInt(velocity * 127f), 1, 127);
        baluController?.HandleNoteOn(note, vel);
    }

    private void OnNoteOff(MidiChannel channel, int note)
    {
        // Note Off � tratado como uma nota com velocidade 0
        baluController?.HandleNoteOn(note, 0);
    }

    // NOVO: M�todo para lidar com eventos de Control Change (como o pedal)
    private void OnControlChange(MidiChannel channel, int controllerNumber, float value)
    {
        // O controlador padr�o para o pedal de sustain � o 64.
        if (controllerNumber == 64)
        {
            // Um valor > 0 (MidiJack usa 0.0 a 1.0) significa que o pedal est� pressionado.
            bool isPedalDown = value > 0;
            baluController?.HandleSustainPedal(isPedalDown);
        }
    }
}