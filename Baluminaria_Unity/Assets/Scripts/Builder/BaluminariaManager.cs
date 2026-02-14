using UnityEngine;
using System.IO;
using System.Linq;

namespace BaluminariaBuilder
{
    public class BaluminariaManager : MonoBehaviour
    {
        public static BaluminariaManager Instance { get; private set; }

        [Header("Pattern")]
        public ColorPattern envelopePattern = ColorPattern.VerticalStripes;
        public bool InvertColors = false;
        public bool InvertDirection = false;
        public Color[] CurrentColors;

        [Range(0, 1)]
        public float AlphaOverride = 1f;

        [Header("Preset")]
        public string CurrentPreset = "Default";
        private string[] _allPresets;

        [Header("Prefab - Segments")]
        public BuilderSegment[] _prefabSegments;
        private BuilderSegment[] _segments = new BuilderSegment[112];

        [SerializeField]
        public Transform _parentSegments;

        [Header("Partes Estáticas")]
        public Renderer bodyBasket;
        public Renderer bodyRopes;
        public Renderer bodySocket;
        public Renderer bodyMouth;
        public Renderer[] bodyRings = new Renderer[7];

        private void Awake()
        {
            Instance = this;
            FixAlpha();
            FindPilotNameListFromDisk();
            LoadPreset();
        }

        [ContextMenu("1. Generate Envelope")]
        public void GenerateEnvelope()
        {
            if (_parentSegments != null)
            {
                for (int i = _parentSegments.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(_parentSegments.GetChild(i).gameObject);
                }
            }
            else
            {
                GameObject p = new GameObject("EnvelopeParent");
                p.transform.SetParent(this.transform);
                _parentSegments = p.transform;
            }

            int index = 0;
            // 7 Linhas
            for (int row = 0; row < 7; row++)
            {
                if (_prefabSegments[row] == null) continue;

                // 16 Colunas
                for (int col = 0; col < 16; col++)
                {
                    BuilderSegment newSegment = Instantiate(_prefabSegments[row], _parentSegments);
                    // Rotaciona 22.5 graus por coluna (360 / 16)
                    newSegment.transform.Rotate(Vector3.up, col * -22.5f);
                    newSegment.gameObject.name = $"Segment_R{row}_C{col}";

                    _segments[index] = newSegment;
                    index++;
                }
            }

            Debug.Log("Envelope gerado com 112 segmentos!");
        }

        private void FindPilotNameListFromDisk()
        {
            string dir = Path.Combine(Application.dataPath, "BaluminariaPresets");
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning("Diretório de presets não encontrado: " + dir);
                return;
            }
            _allPresets = Directory.GetFiles(dir, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f).Replace("_", " "))
                .ToArray();

        }

        private void FixAlpha()
        {
            for (int i = 0; i < CurrentColors.Length; i++)
            {
                Color tempColor = CurrentColors[i];
                tempColor.a = AlphaOverride;
                CurrentColors[i] = tempColor;
            }
        }
        [ContextMenu("2. Apply Pattern")]
        public void ApplyPattern()
        {
            FixAlpha();
            if (_segments[0] == null) return;

            Color[] currentPalette = CurrentColors;
            int color0 = 0;
            int color1 = 1;
            if (InvertColors)
            {
                color0 = 1;
                color1 = 0;
            }
            if (!InvertDirection)
            {
                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 16; col++)
                    {
                        int segmentIndex = row * 16 + col;
                        int colorIndex = 0;
                        int length = currentPalette.Length;

                        switch (envelopePattern)
                        {
                            case ColorPattern.Solid: colorIndex = 0; break;
                            case ColorPattern.VerticalStripes: colorIndex = col % length; break;
                            case ColorPattern.HorizontalStripes: colorIndex = row % length; break;
                            case ColorPattern.DiagonalStripes: colorIndex = (col + row) % length; break;
                            case ColorPattern.DoubleDiagonalStripes: colorIndex = (col + row) % 4 < 2 ? color0 : color1; break;
                            case ColorPattern.Checkerboard: colorIndex = (col / 2 + row / 2) % 2; break;
                            case ColorPattern.PingPong: colorIndex = (col + row) % 4 < 2 ? color0 : color1; break;
                            case ColorPattern.Mandala: colorIndex = (int)(Mathf.Sqrt(col * col + row * row) % 2); break;
                            case ColorPattern.Circles: colorIndex = (int)(Mathf.Sqrt((col - 7.5f) * (col - 7.5f) + (row - 3f) * (row - 3f)) % 2); break;
                        }
                        _segments[segmentIndex].SetColor(currentPalette[colorIndex]);
                    }
                }
            }
            else
            {
                    
                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 16; col++)
                    {
                        int segmentIndex = row * 16 + col;
                        int colorIndex = 0;
                        int length = currentPalette.Length;
                        switch (envelopePattern)
                        {
                            case ColorPattern.Solid: colorIndex = 0; break;
                            case ColorPattern.VerticalStripes: colorIndex = (15 - col) % length; break;
                            case ColorPattern.HorizontalStripes: colorIndex = row % length; break;
                            case ColorPattern.DiagonalStripes: colorIndex = (15 - col + row) % length; break;
                            case ColorPattern.DoubleDiagonalStripes: colorIndex = (15 - col + row) % 4 < 2 ? color0 : color1; break;
                            case ColorPattern.Checkerboard: colorIndex = ((15 - col) / 2 + row / 2) % 2; break;
                            case ColorPattern.PingPong: colorIndex = (15 - col + row) % 4 < 2 ? color0 : color1; break;
                            case ColorPattern.Mandala: colorIndex = (int)(Mathf.Sqrt((15 - col) * (15 - col) + row * row) % 2); break;
                            case ColorPattern.Circles: colorIndex = (int)(Mathf.Sqrt((col - 7.5f) * (col - 7.5f) + (row - 3f) * (row - 3f)) % 2); break;
                        }
                        _segments[segmentIndex].SetColor(currentPalette[colorIndex]);
                    }
                }
            }
        }

        [ContextMenu("4. Save Preset (JSON)")]
        public void SavePreset()
        {
            BaluminariaPreset preset = new BaluminariaPreset();
            preset.pilotName = CurrentPreset;

            for (int i = 0; i < 112; i++)
            {
                if (_segments[i] != null)
                    preset.segmentColors[i] = new ColorData(_segments[i].GetColor());
            }

            // Define um caminho dentro da pasta Assets para facilitar no Editor
            string dir = Path.Combine(Application.dataPath, "BaluminariaPresets");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string safePresetName = CurrentPreset.Replace(" ", "_");
            string filePath = Path.Combine(dir, $"{safePresetName}.json");
            string json = JsonUtility.ToJson(preset, true);

            File.WriteAllText(filePath, json);
            Debug.Log($"<color=green>Preset saved: {filePath}</color>");
        }

        [ContextMenu("5. Load Preset (JSON)")]
        public void LoadPreset()
        {
            GenerateEnvelope(); // Garante que os segmentos existam antes de carregar as cores
            //ApplyPattern(); // Garante que os materiais estejam aplicados antes de carregar as cores
            string safePresetName = CurrentPreset.Replace(" ", "");
            string filePath = Path.Combine(Application.dataPath, "BaluminariaPresets", $"{safePresetName}.json");

            if (!File.Exists(filePath))
            {
                Debug.LogError($"Preset file not found: {filePath}");
                return;
            }

            string json = File.ReadAllText(filePath);
            BaluminariaPreset preset = JsonUtility.FromJson<BaluminariaPreset>(json);

            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i] != null && i < preset.segmentColors.Length)
                {
                    Color c = preset.segmentColors[i].ToColor();
                    c.a = AlphaOverride;
                    _segments[i].SetColor(c);
                }
            }

            Debug.Log($"<color=magenta>Preset '{CurrentPreset}' Loaded! Path:{filePath}</color>");
        }
    }
}