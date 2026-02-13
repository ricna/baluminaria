using UnityEngine;
using UnityEngine.InputSystem;
using BaluminariaBuilder;

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Camera _cam;
    [SerializeField] private BaluminariaManager _manager;

    [Header("Settings")]
    [SerializeField] private Transform _target;
    [SerializeField] private float _distance = 10f;
    [SerializeField] private float _height = 5f;
    [SerializeField] private float _smoothSpeed = 0.125f;
    [SerializeField] private float _zoomSpeed = 10f;

    [Header("Rotation & Sensitivity")]
    public float mouseSensitivityX = 100f;
    public float mouseSensitivityY = 100f;
    [Tooltip("Limites da rotação vertical da câmera.")]
    public Vector2 pitchLimits = new Vector2(-45f, 80f);

    private float _yaw = 0f;
    private float _pitch = 0f;

    [Header("State")]
    [SerializeField] private bool _locked = false; // Se TRUE: mouse livre. Se FALSE: girar camera.
    private bool _canMoveOrZoom = true;

    private void Awake()
    {
        if (_cam == null) _cam = GetComponent<Camera>();

        // Assinando rigorosamente os eventos do seu InputReader
        inputReader.OnLookEvent += HandleLookInput;
        inputReader.OnZoomEvent += HandleZoom;

        // Use o evento de Lock que você configurou no seu InputReader
        inputReader.OnLockCameraEvent += HandleToggleLock;

        // Eventos de clique para o Builder
        inputReader.OnM1Event += () => HandleBuilderAction(true);
        inputReader.OnM2Event += () => HandleBuilderAction(false);
    }

    private void Start()
    {
        if (_target == null && _manager != null) _target = _manager.transform;

        UpdateCursorState();
    }

    private void OnDestroy()
    {
        inputReader.OnLookEvent -= HandleLookInput;
        inputReader.OnZoomEvent -= HandleZoom;
        inputReader.OnLockCameraEvent -= HandleToggleLock;
        // Remova os lambdas se necessário criando funções nomeadas
    }

    private void HandleToggleLock()
    {
        _locked = !_locked;
        UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        if (_locked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // --- LÓGICA DE INTERAÇÃO (BUILDER) ---
    private void HandleBuilderAction(bool isM1)
    {
        if (!_locked) return; // Só interage se a câmera estiver travada

        Ray ray = _cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // 1. Cubo de Paleta (Material)
            if (hit.collider.TryGetComponent(out PaletteCube palette))
            {
                if (isM1) _manager.primaryMaterial = palette.GetMaterial();
                else _manager.secondaryMaterial = palette.GetMaterial();
            }
            // 2. Seletor de Padrão
            else if (hit.collider.TryGetComponent(out PatternSelector pattern))
            {
                _manager.envelopePattern = pattern.patternType;
                _manager.ApplyPattern();
            }
            // 3. Segmento do Balão
            else if (hit.collider.TryGetComponent(out BuilderSegment segment))
            {
                bool isPicking = Keyboard.current.leftShiftKey.isPressed;
                if (isPicking)
                {
                    if (isM1) _manager.primaryMaterial = segment.GetMaterial();
                    else _manager.secondaryMaterial = segment.GetMaterial();
                }
                else
                {
                    Material targetMat = isM1 ? _manager.primaryMaterial : _manager.secondaryMaterial;
                    if (targetMat != null) segment.SetMaterial(targetMat);
                }
            }
        }
    }

    // --- LÓGICA ORIGINAL DE MOVIMENTAÇÃO ---

    private void HandleLookInput(Vector2 lookDelta)
    {
        if (_locked) return; // Impede rotação se estiver usando o mouse no menu

        _yaw += lookDelta.x * mouseSensitivityX * Time.deltaTime;
        _pitch -= lookDelta.y * mouseSensitivityY * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, pitchLimits.x, pitchLimits.y);
    }

    private void HandleZoom(Vector2 zoom)
    {
        if (_canMoveOrZoom)
        {
            _distance -= zoom.y * _zoomSpeed * Time.deltaTime;
            _distance = Mathf.Clamp(_distance, 2f, 50f);
        }
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        if (_canMoveOrZoom)
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            Vector3 desiredPosition = _target.position - (rotation * Vector3.forward * _distance);
            desiredPosition.y += _height;

            // Suavização original mantida
            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed);
            transform.LookAt(_target.position + Vector3.up * (_height / 2));
        }
    }

    public float GetYaw() => _yaw;

    private void OnValidate()
    {
        if (pitchLimits.x > pitchLimits.y)
        {
            float temp = pitchLimits.x;
            pitchLimits.x = pitchLimits.y;
            pitchLimits.y = temp;
        }
    }

    private void OnTriggerEnter(Collider other) => _canMoveOrZoom = false;
    private void OnTriggerExit(Collider other) => _canMoveOrZoom = true;
}