using UnityEngine;
using System.IO;

namespace BaluminariaBuilder
{
    public class BaluminariaManager : MonoBehaviour
    {
        public static BaluminariaManager Instance { get; private set; }

        [Header("Configurações do Piloto")]
        public string pilotName = "NomeDoBalonista";

        [Header("Geração do Envelope")]
        [Tooltip("Arraste os 7 prefabs aqui (do topo até a base, ou vice-versa)")]
        public BuilderSegment[] rowPrefabs = new BuilderSegment[7];
        public Transform envelopeParent;

        [Header("Partes Estáticas")]
        public Renderer bodyBasket;
        public Renderer bodyRopes;
        public Renderer bodySocket;
        public Renderer bodyMouth;
        public Renderer[] bodyRings = new Renderer[7];

        [Header("Pintura e Padrões")]
        public ColorPattern envelopePattern = ColorPattern.VerticalStripes;

        // Armazena a referência dos 112 segmentos gerados
        private BuilderSegment[] _segments = new BuilderSegment[112];

        private void Awake()
        {
            Instance = this;
        }

        [ContextMenu("1. Gerar / Resetar Envelope")]
        public void GenerateEnvelope()
        {
            // Limpa o envelope anterior se existir
            if (envelopeParent != null)
            {
                for (int i = envelopeParent.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(envelopeParent.GetChild(i).gameObject);
                }
            }
            else
            {
                GameObject p = new GameObject("EnvelopeParent");
                p.transform.SetParent(this.transform);
                envelopeParent = p.transform;
            }

            int index = 0;
            // 7 Linhas
            for (int row = 0; row < 7; row++)
            {
                if (rowPrefabs[row] == null) continue;

                // 16 Colunas
                for (int col = 0; col < 16; col++)
                {
                    BuilderSegment newSegment = Instantiate(rowPrefabs[row], envelopeParent);
                    // Rotaciona 22.5 graus por coluna (360 / 16)
                    newSegment.transform.Rotate(Vector3.up, col * -22.5f);
                    newSegment.gameObject.name = $"Segment_R{row}_C{col}";

                    _segments[index] = newSegment;
                    index++;
                }
            }

            Debug.Log("Envelope gerado com 112 segmentos!");
        }

        [Header("Materiais Atuais (M1 e M2)")]
        public Material primaryMaterial;
        public Material secondaryMaterial;

        [ContextMenu("2. Aplicar Padrão de Materiais")]
        public void ApplyPattern()
        {
            if (_segments[0] == null) return;

            Material[] currentPalette = new Material[] { primaryMaterial, secondaryMaterial };

            for (int row = 0; row < 7; row++)
            {
                for (int col = 0; col < 16; col++)
                {
                    int index = row * 16 + col;
                    int matIndex = 0;

                    switch (envelopePattern)
                    {
                        case ColorPattern.Solid: matIndex = 0; break;
                        case ColorPattern.VerticalStripes: matIndex = col % 2; break;
                        case ColorPattern.HorizontalStripes: matIndex = row % 2; break;
                        case ColorPattern.DiagonalStripes: matIndex = (col + row) % 2; break;
                    }

                    if (currentPalette[matIndex] != null)
                        _segments[index].SetMaterial(currentPalette[matIndex]);
                }
            }
        }
        private void SetRendererColor(Renderer r, Color c)
        {
            if (r != null)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c); // Certifique-se que seu shader usa _BaseColor
                r.SetPropertyBlock(mpb);
            }
        }

        [ContextMenu("4. Salvar Preset (JSON)")]
        public void SavePreset()
        {
            BaluminariaPreset preset = new BaluminariaPreset();
            preset.pilotName = pilotName;

            for (int i = 0; i < 112; i++)
            {
                if (_segments[i] != null)
                    preset.segmentColors[i] = new ColorData(_segments[i].GetColor());
            }

            // Define um caminho dentro da pasta Assets para facilitar no Editor
            string dir = Path.Combine(Application.dataPath, "BaluminariaPresets");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string filePath = Path.Combine(dir, $"{pilotName.Replace(" ", "_")}.json");
            string json = JsonUtility.ToJson(preset, true);

            File.WriteAllText(filePath, json);
            Debug.Log($"Mockup salvo para o piloto: {filePath}");
        }

        [ContextMenu("5. Carregar Preset (JSON)")]
        public void LoadPreset()
        {
            GenerateEnvelope(); // Garante que os segmentos existam antes de carregar as cores
            ApplyPattern(); // Garante que os materiais estejam aplicados antes de carregar as cores
            string filePath = Path.Combine(Application.dataPath, "BaluminariaPresets", $"{pilotName.Replace(" ", "_")}.json");

            if (!File.Exists(filePath))
            {
                Debug.LogError("Arquivo não encontrado: " + filePath);
                return;
            }

            string json = File.ReadAllText(filePath);
            BaluminariaPreset preset = JsonUtility.FromJson<BaluminariaPreset>(json);

            // Aplica as cores nos segmentos
            for (int i = 0; i < _segments.Length; i++)
            {
                if (_segments[i] != null && i < preset.segmentColors.Length)
                {
                    Color corCarregada = preset.segmentColors[i].ToColor();
                    // Segurança: Se o alpha for 0, força 1 (evita objeto invisível)
                    if (corCarregada.a <= 0) corCarregada.a = 1f;

                    _segments[i].SetColor(corCarregada);
                }
            }

            Debug.Log("Preset carregado com sucesso!");
        }
    }
}