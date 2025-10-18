using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Configurações da Câmera")]
    [Tooltip("O GameObject que a câmera seguirá.")]
    public Transform target;
    [Tooltip("Distância da câmera em relação ao alvo.")]
    public float distance = 10f;
    [Tooltip("Altura da câmera em relação ao alvo.")]
    public float height = 5f;
    [Tooltip("Velocidade de suavização do movimento da câmera.")]
    public float smoothSpeed = 0.125f;
    [Tooltip("Sensibilidade da rotação horizontal do mouse.")]
    public float mouseSensitivityX = 100f;
    [Tooltip("Sensibilidade da rotação vertical do mouse.")]
    public float mouseSensitivityY = 100f;
    [Tooltip("Limites da rotação vertical da câmera (evita inversão).")]
    public Vector2 pitchLimits = new Vector2(-45f, 80f); // X = min, Y = max

    private BalloonInputActions _inputActions;
    private float _yaw = 0f; // Rotação horizontal
    private float _pitch = 0f; // Rotação vertical

    private void Awake()
    {
        _inputActions = new BalloonInputActions();
        _inputActions.BalloonControls.Look.performed += ctx => HandleLookInput(ctx.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        _inputActions.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        _inputActions.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void HandleLookInput(Vector2 lookDelta)
    {
        _yaw += lookDelta.x * mouseSensitivityX * Time.deltaTime;
        _pitch -= lookDelta.y * mouseSensitivityY * Time.deltaTime;

        _pitch = Mathf.Clamp(_pitch, pitchLimits.x, pitchLimits.y);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        Vector3 desiredPosition = target.position - (rotation * Vector3.forward * distance);
        desiredPosition.y += height;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        transform.LookAt(target.position + Vector3.up * (height / 2));
    }

    public float GetYaw()
    {
        return _yaw;
    }
}