
public class Segment : MonoBehaviour
{
    [SerializeField]
    private Light _light;
    [SerializeField]
    private BoxCollider _boxCollider;

    public Color CurrentColor
    {
        get { return _light.color; }
        set { _light.color = value; }
    }

    private void Awake()
    {
        // � melhor inicializar os componentes no Awake para garantir que estejam prontos
        AddLight();
        AddBoxCollider();
    }
    
    // O m�todo Start pode ser removido ou deixado vazio se a inicializa��o for movida para o Awake
    // private void Start() { }

    private void AddBoxCollider()
    {
        if (_boxCollider == null)
        {
            _boxCollider = gameObject.AddComponent<BoxCollider>();
            _boxCollider.size = new Vector3(1f, 1f, 1f);
            _boxCollider.center = new Vector3(0f, 0.5f, 0f);
            _boxCollider.isTrigger = true;
        }
    }

    private void AddLight()
    {
        if (_light == null)
        {
            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = 5f;
            _light.intensity = 1f;
            _light.color = Color.white;
            if (_boxCollider != null)
            {
                _light.transform.position = _boxCollider.bounds.center;
            }
            else
            {
                _light.transform.localPosition = Vector3.zero;
            }
        }
    }

    // Este m�todo agora � redundante devido � propriedade CurrentColor, mas pode ser mantido por compatibilidade.
    public void ChangeLightColor(Color color)
    {
        _light.color = color;
    }
}