using UnityEngine;

public class Baluminaria : MonoBehaviour
{
    [SerializeField]
    private InputReader _inputReader;
    private Transform _transform;
    [SerializeField]
    private bool _autoRotate = true; // Agora público para controle externo
    [SerializeField]
    private float _rotationSpeed = 10f;
    [SerializeField]
    private Segment[] _prefabSegments; // Array para os 7 prefabs de segmento
    private Segment[] _allSegments;

    [Header("Configurações de Luz")]
    [Tooltip("A intensidade máxima que as luzes podem atingir. O valor da velocidade MIDI será multiplicado por essa intensidade.")]
    [Range(0f, 1f)]
    public float maxIntensity = 0.5f;
    public InputReader InputReader
    {
        get
        {
            return _inputReader;
        }
        private set
        {
            _inputReader = value;
        }
    }

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (!_autoRotate) return;
        _transform = transform;
        _transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
    }



    private void Initialize()
    {
        if (_allSegments != null && _allSegments.Length > 0)
        {
            return;
        }

        _allSegments = new Segment[112]; // 16 segmentos * 7 fileiras

        int segmentIndex = 0;
        for (int i = 0; i < 16; i++)
        {
            foreach (var segment in _prefabSegments)
            {
                var newSegment = Instantiate(segment, transform);
                newSegment.transform.Rotate(Vector3.up, i * -22.5f);
                _allSegments[segmentIndex] = newSegment;
                newSegment.ChangeLightColor(Color.black);
                newSegment.SetIntensity(0f);
                segmentIndex++;
            }
        }
    }

    public void ChangeSegmentLightColor(Color color)
    {
        foreach (var segment in _allSegments)
        {
            if (segment != null)
                segment.ChangeLightColor(color);
        }
    }

    public Segment[] GetSegments()
    {
        if (_allSegments == null || _allSegments.Length == 0)
        {
            Initialize();
        }
        return _allSegments;
    }

    // Método público para controlar a rotação automática (chamado pelo GameManager)
    public void SetAutoRotate(bool rotate)
    {
        _autoRotate = rotate;
    }
}