// ScoreGenerator.cs
using MidiPlayerTK;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using UnityEngine;


[Serializable]
public class ScoreNote
{
    public int MidiNote;
    public int Velocity;
    public double StartTime; // Em segundos
    public double Duration; // Em segundos
}

public class ScoreGenerator : MonoBehaviour
{
    [Header("Configurações da Partitura")]
    [Tooltip("BPM para cálculo de duração das notas. Se 0, será detectado do MIDI.")]
    [SerializeField]
    private float _bpm = 120f;
    [Tooltip("Fórmula de compasso. Ex: 4 para 4/4")]
    [SerializeField]
    private int _timeSignatureNumerator = 4;
    [Tooltip("Valor da nota que recebe uma batida. Ex: 4 para 4/4 (semínima)")]
    [SerializeField]
    private int _timeSignatureDenominator = 4;
    [SerializeField]
    private List<ScoreNote> _scoreNotes = new List<ScoreNote>();
    private double _lastEventTime = 0;
    [SerializeField]
    private Dictionary<int, double> _activeNoteTimes = new Dictionary<int, double>();

    // Este método é chamado pelo BaluMidiController
    public void AddNoteEvent(MPTKEvent noteEvent)
    {
        double currentTime = Time.timeAsDouble;

        // Se a nota está ligada
        if (noteEvent.Velocity > 0)
        {
            _activeNoteTimes[noteEvent.Value] = currentTime;
        }
        // Se a nota está desligada
        else if (_activeNoteTimes.ContainsKey(noteEvent.Value))
        {
            double startTime = _activeNoteTimes[noteEvent.Value];
            double duration = currentTime - startTime;
            Debug.Log($"AddNoteEvent{noteEvent.Value} duration{duration}");

            _scoreNotes.Add(new ScoreNote
            {
                MidiNote = noteEvent.Value,
                Velocity = noteEvent.Velocity,
                StartTime = startTime,
                Duration = duration
            });

            _activeNoteTimes.Remove(noteEvent.Value);
        }
    }

    // Método para gerar o arquivo MusicXML
    public string GenerateMusicXML()
    {
        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineOnAttributes = true
        };

        using (var writer = XmlWriter.Create(sb, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("score-partwise");
            writer.WriteAttributeString("version", "3.1");

            // Informações da partitura
            writer.WriteStartElement("part-list");
            writer.WriteStartElement("score-part");
            writer.WriteAttributeString("id", "P1");
            writer.WriteElementString("part-name", "Piano");
            writer.WriteEndElement(); // score-part
            writer.WriteEndElement(); // part-list

            // Início da parte musical
            writer.WriteStartElement("part");
            writer.WriteAttributeString("id", "P1");

            // Início do primeiro compasso
            writer.WriteStartElement("measure");
            writer.WriteAttributeString("number", "1");

            // Clave e compasso
            writer.WriteStartElement("attributes");
            writer.WriteStartElement("divisions"); writer.WriteString("256"); writer.WriteEndElement(); // Unidade de tempo para a duração das notas
            writer.WriteStartElement("key");
            writer.WriteElementString("fifths", "0");
            writer.WriteEndElement();
            writer.WriteStartElement("time");
            writer.WriteElementString("beats", _timeSignatureNumerator.ToString());
            writer.WriteElementString("beat-type", _timeSignatureDenominator.ToString());
            writer.WriteEndElement();
            writer.WriteStartElement("clef");
            writer.WriteElementString("sign", "G");
            writer.WriteElementString("line", "2");
            writer.WriteEndElement();
            writer.WriteEndElement(); // attributes

            // Processar e escrever as notas
            var noteQuantizer = new NoteQuantizer(_bpm, _timeSignatureDenominator);
            foreach (var note in _scoreNotes)
            {
                writer.WriteStartElement("note");

                // Representação da nota
                writer.WriteStartElement("pitch");
                writer.WriteElementString("step", noteQuantizer.GetNoteName(note.MidiNote));
                writer.WriteElementString("octave", noteQuantizer.GetOctave(note.MidiNote).ToString());
                writer.WriteEndElement(); // pitch

                // Duração da nota
                writer.WriteElementString("duration", noteQuantizer.GetMusicXMLDuration(note.Duration).ToString());
                writer.WriteElementString("type", noteQuantizer.GetMusicXMLType(note.Duration));

                writer.WriteEndElement(); // note
            }

            writer.WriteEndElement(); // measure
            writer.WriteEndElement(); // part
            writer.WriteEndElement(); // score-partwise
            writer.WriteEndDocument();
        }

        return sb.ToString();
    }

    // Método para salvar o arquivo MusicXML em um diretório
    public void SaveMusicXMLFile(string filename = "partitura.xml")
    {
        string musicXML = GenerateMusicXML();
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
        System.IO.File.WriteAllText(filePath, musicXML);
        Debug.Log($"Arquivo MusicXML salvo em: {filePath}");
    }
}