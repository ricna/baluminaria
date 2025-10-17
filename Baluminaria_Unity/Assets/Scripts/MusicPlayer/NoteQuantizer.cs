// NoteQuantizer.cs
using System.Collections.Generic;
using UnityEngine;

public class NoteQuantizer
{
    private float _bpm;
    private float _beatDuration; // Duração de uma batida em segundos
    private Dictionary<string, int> _noteDurations = new Dictionary<string, int>();

    public NoteQuantizer(float bpm, int denominator)
    {
        _bpm = bpm;
        _beatDuration = 60f / _bpm;

        // Definindo a duração de cada tipo de nota em segundos
        float semibreveDuration = _beatDuration * (4f / denominator);

        _noteDurations["whole"] = (int)(semibreveDuration * 256);
        _noteDurations["half"] = (int)(semibreveDuration / 2 * 256);
        _noteDurations["quarter"] = (int)(semibreveDuration / 4 * 256);
        _noteDurations["eighth"] = (int)(semibreveDuration / 8 * 256);
        _noteDurations["16th"] = (int)(semibreveDuration / 16 * 256);
        _noteDurations["32nd"] = (int)(semibreveDuration / 32 * 256);
    }

    public string GetNoteName(int midiNote)
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        return noteNames[midiNote % 12];
    }

    public int GetOctave(int midiNote)
    {
        return (midiNote / 12) - 1;
    }

    public int GetMusicXMLDuration(double noteDurationInSeconds)
    {
        // Encontra a duração rítmica mais próxima
        float targetDuration = (float)noteDurationInSeconds;
        int bestMatch = _noteDurations["quarter"]; // Default
        float minDifference = Mathf.Abs(targetDuration - (_noteDurations["quarter"] / 256f));

        foreach (var pair in _noteDurations)
        {
            float durationSec = pair.Value / 256f;
            float difference = Mathf.Abs(targetDuration - durationSec);
            if (difference < minDifference)
            {
                minDifference = difference;
                bestMatch = pair.Value;
            }
        }
        return bestMatch;
    }

    public string GetMusicXMLType(double noteDurationInSeconds)
    {
        // Retorna o nome do tipo de nota (whole, half, quarter, etc.)
        float targetDuration = (float)noteDurationInSeconds;
        string bestMatch = "quarter";
        float minDifference = Mathf.Abs(targetDuration - (_noteDurations["quarter"] / 256f));

        foreach (var pair in _noteDurations)
        {
            float durationSec = pair.Value / 256f;
            float difference = Mathf.Abs(targetDuration - durationSec);
            if (difference < minDifference)
            {
                minDifference = difference;
                bestMatch = pair.Key;
            }
        }
        return bestMatch;
    }
}