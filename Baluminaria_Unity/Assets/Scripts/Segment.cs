using UnityEngine;

public class Segment : MonoBehaviour
{
    // Propriedades da Luz
    [Header("Light Properties")]
    [SerializeField] private Light _light;
    [SerializeField] private float _range = 1f;

    // Propriedades do Material (Shader Graph)
    [Header("Material Properties")]
    [SerializeField] private Color _baseColor = Color.white;
    [SerializeField] private Color _subsurfaceColor = Color.white;
    [SerializeField] private float _translucencyPower = 1.0f;
    [SerializeField] private float _translucencyStrength = 5.0f;

    // Componentes e MaterialPropertyBlock
    private Renderer _renderer;
    private MaterialPropertyBlock _materialPropertyBlock;

    // IDs para as propriedades do shader (mais eficientes que strings)
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int SubsurfaceColorID = Shader.PropertyToID("_SubsurfaceColor");
    private static readonly int TranslucencyPowerID = Shader.PropertyToID("_TranslucencyPower");
    private static readonly int TranslucencyStrengthID = Shader.PropertyToID("_TranslucencyStrength");

    private void Awake()
    {
        // 1. Configuração da Luz
        if (_light == null)
        {
            _light = GetComponentInChildren<Light>();
        }
        if (_light != null)
        {
            _light.range = _range;
            _light.color = _baseColor; // Inicializa a cor da luz com a cor base do material, se desejar
        }

        // 2. Configuração do Material
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError("Renderer component not found on this GameObject. Material properties cannot be set.", this);
        }
        else
        {
            _materialPropertyBlock = new MaterialPropertyBlock();
            // Aplica as propriedades do material no Awake para garantir que elas sejam definidas
            // com os valores do Inspector ou default.
            ApplyMaterialProperties();
        }
    }

    // --- Métodos de Controle da Luz ---
    public Color CurrentLightColor
    {
        get { return _light != null ? _light.color : Color.black; }
        set { if (_light != null) _light.color = value; }
    }

    public void ChangeLightColor(Color color)
    {
        CurrentLightColor = color;
    }

    public void SetLightIntensity(float intensity)
    {
        if (_light != null)
        {
            _light.intensity = intensity;
        }
    }

    // --- Métodos de Controle do Material ---
    public Color CurrentBaseColor
    {
        get { return _baseColor; }
        set
        {
            _baseColor = value;
            ApplyMaterialProperties();
        }
    }

    public Color CurrentSubsurfaceColor
    {
        get { return _subsurfaceColor; }
        set
        {
            _subsurfaceColor = value;
            ApplyMaterialProperties();
        }
    }

    public void SetMaterialColors(Color newBaseColor, Color newSubsurfaceColor)
    {
        _baseColor = newBaseColor;
        _subsurfaceColor = newSubsurfaceColor;
        ApplyMaterialProperties();
    }

    public void SetMaterialTranslucency(float power, float strength)
    {
        _translucencyPower = power;
        _translucencyStrength = strength;
        ApplyMaterialProperties();
    }

    // Método privado para aplicar todas as propriedades do MaterialPropertyBlock
    private void ApplyMaterialProperties()
    {
        if (_renderer == null) return;

        // Garante que o bloco não é nulo
        if (_materialPropertyBlock == null)
        {
            _materialPropertyBlock = new MaterialPropertyBlock();
        }

        // Pega o bloco de propriedades atual do Renderer
        _renderer.GetPropertyBlock(_materialPropertyBlock);

        // Define as novas propriedades
        _materialPropertyBlock.SetColor(BaseColorID, _baseColor);
        _materialPropertyBlock.SetColor(SubsurfaceColorID, _subsurfaceColor);
        _materialPropertyBlock.SetFloat(TranslucencyPowerID, _translucencyPower);
        _materialPropertyBlock.SetFloat(TranslucencyStrengthID, _translucencyStrength);

        // Aplica o bloco de volta ao Renderer desta instância
        _renderer.SetPropertyBlock(_materialPropertyBlock);
    }
}