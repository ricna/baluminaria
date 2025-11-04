using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private InputReader inputReader;
    [SerializeField]
    private SphereCollider _collider;

    [Header("Settings")]
    [SerializeField]
    private Baluminaria _baluminaria;
    [SerializeField]
    private Transform _target;
    [SerializeField]
    private float _distance = 10f;
    [SerializeField]
    private float _height = 5f;
    [SerializeField]
    private float _smoothSpeed = 0.125f;
    [SerializeField]
    private float _zoomSpeed = 10f;

    public float mouseSensitivityX = 100f;
    public float mouseSensitivityY = 100f;
    [Tooltip("Limites da rotação vertical da câmera (evita inversão).")]
    public Vector2 pitchLimits = new Vector2(-45f, 80f); // X = min, Y = max
    private float _yaw = 0f; // Rotação horizontal
    private float _pitch = 0f; // Rotação vertical


    private bool _canMoveOrZoom = true;
    private bool _locked = false;

    private Vector2 _positionLocked = Vector2.zero;
    private Quaternion _rotationLocked = Quaternion.identity;



    private void Awake()
    {
        inputReader.OnLookEvent += HandleLookInput;
        inputReader.OnZoomEvent += HandleZoom;
        if (_target == null)
        {
            _target = _baluminaria.transform;
        }
    }

    private void Start()
    {
        if (_collider == null)
        {
            _collider = GetComponent<SphereCollider>();
        }
    }

    private void OnDestroy()
    {
        inputReader.OnLookEvent -= HandleLookInput;
        inputReader.OnZoomEvent -= HandleZoom;
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleLookInput(Vector2 lookDelta)
    {
        _yaw += lookDelta.x * mouseSensitivityX * Time.deltaTime;
        _pitch -= lookDelta.y * mouseSensitivityY * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, pitchLimits.x, pitchLimits.y);
    }

    private void HandleZoom(Vector2 zoom)
    {
        if (_canMoveOrZoom)
        {
            _distance -= zoom.y * _zoomSpeed * Time.deltaTime;
            _distance = Mathf.Clamp(_distance, 2f, 50f); // Limita a distância da câmera
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

            transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed);
            transform.LookAt(_target.position + Vector3.up * (_height / 2));
        }
        if (!_canMoveOrZoom)
        {
            //Unstuck();
        }
    }

    private void Unstuck()
    {
        Vector3 directionToTarget = (_target.position - transform.position).normalized;
        transform.position += directionToTarget * _zoomSpeed * Time.deltaTime;
        transform.LookAt(_target.position + Vector3.up * (_height / 2));
    }

    private void OnValidate()
    {
        if (pitchLimits.x > pitchLimits.y)
        {
            float temp = pitchLimits.x;
            pitchLimits.x = pitchLimits.y;
            pitchLimits.y = temp;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        _canMoveOrZoom = false;
    }

    private void OnTriggerExit(Collider other)
    {
        _canMoveOrZoom = true;
    }

    public float GetYaw()
    {
        return _yaw;
    }



}