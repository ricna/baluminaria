using System.Collections.Generic;
using UnityEngine;

public class VisualManager : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject notePrefab;

    [Header("Pooling")]
    public int preloadCount = 64;
    private Queue<GameObject> pool;

    [Header("Layout")]
    public Transform keyboardOrigin; // Canto/âncora do teclado na cena (ex: canto inferior esquerdo da tecla C1)
    public float whiteKeyWidth = 0.0224f; // Largura de uma tecla branca (para cálculo de espaçamento)
    public float blackKeyWidthMultiplier = 0.6f; // Teclas pretas são um % da largura das brancas
    public float whiteKeySpacing = 0.024f; // Espaçamento entre as teclas brancas (centro a centro)
    public float blackKeyOffsetFromWhite = 0.012f; // Offset da tecla preta em relação ao "centro" da tecla branca adjacente
    public float whiteKeyYPosition = 0.0f; // Altura base das notas brancas
    public float blackKeyYPosition = 0.005f; // Altura base das notas pretas (ligeiramente elevadas)
    public float maxNoteScaleY = 2.0f; // Altura máxima que a nota pode atingir (passado para NoteVisual)

    [Header("Coloring")]
    public Gradient noteColorGradient; // Mapeia velocity para cor

    [Header("Effects")]
    public ParticleSystem embersPS;

    private Dictionary<int, GameObject> activeNotes;

    // Conjunto de notas MIDI que são teclas pretas
    private readonly HashSet<int> _blackKeys = new HashSet<int>
    {
        1, 3, 6, 8, 10, 13, 15, 18, 20, 22, 25, 27, 30, 32, 34, 37, 39, 42, 44, 46,
        49, 51, 54, 56, 58, 61, 63, 66, 68, 70, 73, 75, 78, 80, 82, 85, 87, 90, 92,
        94, 97, 99, 102, 104, 106, 109, 111, 114, 116, 118, 121, 123, 126
    };

    private void Awake()
    {
        this.pool = new Queue<GameObject>();
        this.activeNotes = new Dictionary<int, GameObject>();

        for (int i = 0; i < this.preloadCount; i++)
        {
            GameObject go = Instantiate(this.notePrefab, transform); // Torna o VisualManager pai
            go.SetActive(false);
            this.pool.Enqueue(go);
        }
    }

    private void OnEnable()
    {
        MagicNotes_MIDI.OnNoteOn += OnNoteOnHandler;
        MagicNotes_MIDI.OnNoteOff += OnNoteOffHandler;
    }

    private void OnDisable()
    {
        MagicNotes_MIDI.OnNoteOn -= OnNoteOnHandler;
        MagicNotes_MIDI.OnNoteOff -= OnNoteOffHandler;
    }

    private void OnNoteOnHandler(int note, int velocity)
    {
        if (this.activeNotes.ContainsKey(note))
        {
            // Se a nota já está ativa, você pode querer reiniciar ou ignorar.
            // Para "SeeMusic", ignorar é o comportamento comum.
            return;
        }

        GameObject go;
        if (this.pool.Count > 0)
        {
            go = this.pool.Dequeue();
        }
        else
        {
            go = Instantiate(this.notePrefab, transform);
        }

        go.SetActive(true);

        Vector3 pos = CalculatePositionForNote(note);
        go.transform.position = pos;
        // A rotação local será padrão, a rotação global pode ser a do VisualManager
        go.transform.localRotation = Quaternion.identity;

        NoteVisual nv = go.GetComponent<NoteVisual>();
        if (nv == null)
        {
            Debug.LogError("NotePrefab does not have a NoteVisual component!", notePrefab);
            go.SetActive(false);
            this.pool.Enqueue(go);
            return;
        }

        float velNorm = Mathf.Clamp01((float)velocity / 127.0f);
        Color col = this.noteColorGradient.Evaluate(velNorm);
        bool isBlackKey = IsBlackKey(note);

        // Inicializa o visual da nota, que já começa a crescer e se mover no Update do NoteVisual
        nv.Init(note, velNorm, col, this.maxNoteScaleY, isBlackKey, this.blackKeyWidthMultiplier);

        this.activeNotes.Add(note, go);

        if (this.embersPS != null)
        {
            this.embersPS.transform.position = pos;
            this.embersPS.Emit(8);
        }
    }

    private void OnNoteOffHandler(int note)
    {
        if (!this.activeNotes.TryGetValue(note, out GameObject go))
        {
            return;
        }

        NoteVisual nv = go.GetComponent<NoteVisual>();
        if (nv == null)
        {
            Debug.LogError($"Note {note} in activeNotes did not have a NoteVisual component!");
            go.SetActive(false);
            this.pool.Enqueue(go);
        }
        else
        {
            nv.Release(); // A animação de fade e retorno ao pool agora é gerenciada pelo NoteVisual
        }

        this.activeNotes.Remove(note);
    }

    // Método auxiliar para o NoteVisual retornar ao pool, se ele usar o pool do VisualManager
    public void ReturnNoteToPool(GameObject noteObject)
    {
        // Só retorna ao pool se o objeto ainda não foi desativado (por exemplo, por um MagicNotes_Pool)
        if (noteObject.activeSelf)
        {
            noteObject.SetActive(false);
            // Re-define a escala X para o padrão do prefab antes de retornar ao pool
            noteObject.transform.localScale = notePrefab.transform.localScale;
            this.pool.Enqueue(noteObject);
        }
    }

    private Vector3 CalculatePositionForNote(int midiNote)
    {
        // MIDI note 60 é o C4 (Middle C)
        Vector3 origin = (this.keyboardOrigin != null) ? this.keyboardOrigin.position : Vector3.zero;

        bool isBlackKey = IsBlackKey(midiNote);
        float xOffset = 0f;
        float yPosition = isBlackKey ? blackKeyYPosition : whiteKeyYPosition;

        // Ponto de referência, por exemplo, C1 (MIDI 36)
        int referenceMidiNote = 36;

        int whiteKeysPassed = 0;
        for (int i = referenceMidiNote; i < midiNote; i++)
        {
            if (!IsBlackKey(i))
            {
                whiteKeysPassed++;
            }
        }

        xOffset = (whiteKeysPassed * whiteKeySpacing);

        if (isBlackKey)
        {
            // Para teclas pretas, ajustamos sua posição X.
            // A lógica é posicionar a tecla preta entre duas teclas brancas.
            // C# (midiNote = 61) fica entre C (midiNote = 60) e D (midiNote = 62)
            // D# (midiNote = 63) fica entre D (midiNote = 62) e E (midiNote = 64)
            // F# (midiNote = 66) fica entre F (midiNote = 65) e G (midiNote = 67)
            // E assim por diante.

            // Encontra a tecla branca à esquerda para posicionamento
            int whiteKeyToLeft = midiNote;
            while (IsBlackKey(whiteKeyToLeft) && whiteKeyToLeft > 0)
            {
                whiteKeyToLeft--;
            }

            int whiteKeysBeforeThisBlack = 0;
            for (int i = referenceMidiNote; i < whiteKeyToLeft; i++)
            {
                if (!IsBlackKey(i))
                {
                    whiteKeysBeforeThisBlack++;
                }
            }

            // Posiciona a tecla preta ligeiramente à direita do centro da tecla branca à sua esquerda
            // e aplica um offset para que ela fique visualmente no meio das duas brancas.
            xOffset = (whiteKeysBeforeThisBlack * whiteKeySpacing) + (whiteKeySpacing / 2f) + blackKeyOffsetFromWhite;
        }

        return new Vector3(origin.x + xOffset, origin.y + yPosition, origin.z);
    }

    private bool IsBlackKey(int midiNote)
    {
        int noteInOctave = midiNote % 12;
        return _blackKeys.Contains(noteInOctave);
    }

    public void ClearAll()
    {
        List<int> keys = new List<int>(this.activeNotes.Keys);
        foreach (int note in keys)
        {
            OnNoteOffHandler(note);
        }
        this.activeNotes.Clear();
    }
}