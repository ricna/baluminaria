using UnityEngine;
using System.Collections; // Necessário para coroutines, mesmo que eu remova-as nesta versão

public class NoteVisual : MonoBehaviour
{
    public int midiNote;
    public float velocityNormalized; // 0..1
    public float growSpeed = 1.0f; // Velocidade de crescimento vertical (scale)
    public float maxScaleY = 1.0f; // Altura máxima que a nota pode atingir
    public float moveSpeedZ = 1.0f; // Velocidade de movimento para frente (localPosition.z)
    public float fadeDuration = 0.4f; // Duração do fade out ao soltar
    public string poolId; // Se estiver usando pool (MagicNotes_Pool)

    private bool isReleased = false;
    private float timeSinceOn = 0.0f; // Tempo desde que a nota foi pressionada
    private float timeSinceRelease = 0.0f; // Tempo desde que a nota foi solta

    private Renderer rend;
    private Color baseColor; // Cor inicial da nota

    // Propriedades para rastrear o estado inicial para reset
    private Vector3 initialLocalScale;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalPosition;

    // Adicionado para lidar com a diferença de largura de teclas pretas
    private bool isBlackKey = false;
    private float whiteKeyDefaultScaleX; // Para resetar a largura
    private float blackKeyScaleXMultiplier; // Para aplicar a largura da tecla preta

    private void Awake()
    {
        this.rend = GetComponent<Renderer>();
        if (this.rend != null)
        {
            this.baseColor = this.rend.material.GetColor("_Color");
        }

        // Guarda o estado inicial do prefab para resetar ao retornar ao pool
        this.initialLocalScale = this.transform.localScale;
        this.initialLocalRotation = this.transform.localRotation;
        this.initialLocalPosition = this.transform.localPosition;

        // Se o prefab já tem uma largura padrão, guarde-a
        this.whiteKeyDefaultScaleX = this.transform.localScale.x;
    }

    private void OnEnable()
    {
        // Resetar o estado ao ser ativado (vindo do pool)
        this.isReleased = false;
        this.timeSinceOn = 0.0f;
        this.timeSinceRelease = 0.0f;

        // Resetar transformações e renderização
        this.transform.localScale = this.initialLocalScale;
        this.transform.localRotation = this.initialLocalRotation;
        // A posição global será definida pelo VisualManager, mas localPosition deve ser 0
        this.transform.localPosition = Vector3.zero;

        if (this.rend != null)
        {
            this.rend.enabled = true;
            // Garante que a opacidade esteja total ao iniciar
            Color c = this.rend.material.GetColor("_Color");
            c.a = 1.0f;
            this.rend.material.SetColor("_Color", c);
        }
    }

    public void Init(int note, float velocityNorm, Color color, float targetMaxScaleY, bool isBlackKey, float blackKeyWidthMultiplier)
    {
        this.midiNote = note;
        this.velocityNormalized = velocityNorm;
        this.baseColor = color;
        this.maxScaleY = targetMaxScaleY;
        this.isBlackKey = isBlackKey;
        this.blackKeyScaleXMultiplier = blackKeyWidthMultiplier;

        if (this.rend != null)
        {
            this.rend.material.SetColor("_Color", this.baseColor);
        }

        // Ajusta a largura da nota com base se é tecla preta ou branca
        Vector3 currentScale = this.transform.localScale;
        if (this.isBlackKey)
        {
            currentScale.x = this.whiteKeyDefaultScaleX * this.blackKeyScaleXMultiplier;
        }
        else
        {
            currentScale.x = this.whiteKeyDefaultScaleX;
        }
        this.transform.localScale = currentScale;
    }

    private void Update()
    {
        // === ANIMAÇÃO DE CRESCIMENTO E MOVIMENTO PARA FRENTE ===
        // Isso acontece sempre, até que a nota seja liberada.
        if (!this.isReleased)
        {
            this.timeSinceOn += Time.deltaTime;

            // 1. Crescimento vertical (scale.y)
            // A altura desejada aumenta com o tempo, limitada por maxScaleY
            // A velocidade de crescimento pode ser influenciada pela velocity da nota
            float currentDesiredScaleY = Mathf.Min(this.maxScaleY, this.initialLocalScale.y + (this.timeSinceOn * this.growSpeed * (0.5f + this.velocityNormalized * 0.5f)));
            Vector3 currentScale = this.transform.localScale;
            currentScale.y = currentDesiredScaleY;
            this.transform.localScale = currentScale;
        }
        else
        {
            // === ANIMAÇÃO DE FADE OUT E CONTINUAÇÃO DO MOVIMENTO ===
            this.timeSinceRelease += Time.deltaTime;
            float fadeProgress = Mathf.Clamp01(this.timeSinceRelease / this.fadeDuration);

            if (this.rend != null)
            {
                Color c = this.rend.material.GetColor("_Color");
                c.a = Mathf.Lerp(1.0f, 0.0f, fadeProgress);
                this.rend.material.SetColor("_Color", c);
            }

            // Se o fade estiver completo, limpar e retornar ao pool
            if (fadeProgress >= 1.0f)
            {
                this.Cleanup();
            }
        }

        // 2. Movimento para frente (localPosition.z)
        // Isso acontece continuamente, tanto pressionada quanto liberada.
        // A velocidade pode ser uma constante global ou influenciada por algo mais.
        // Assumimos que a 'frente' é ao longo do eixo Z local do objeto.
        Vector3 currentPosition = this.transform.localPosition;
        currentPosition.z += Time.deltaTime * this.moveSpeedZ;
        this.transform.localPosition = currentPosition;
    }

    public void Release()
    {
        this.isReleased = true;
        this.timeSinceRelease = 0.0f; // Reinicia o contador para o fade out
    }

    private void Cleanup()
    {
        // Certifica-se de que o renderizador esteja desativado quando não estiver em uso
        if (this.rend != null)
        {
            this.rend.enabled = false;
        }

        // Retorna para o pool ou destrói
        if (MagicNotes_Pool.Instance != null && string.IsNullOrEmpty(this.poolId) == false)
        {
            MagicNotes_Pool.Instance.Despawn(this.poolId, this.gameObject);
        }
        else
        {
            // Se não houver pool ou poolId, apenas desativa o GameObject
            this.gameObject.SetActive(false);
        }
    }
}