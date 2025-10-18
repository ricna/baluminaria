using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [Header("Configura��es da C�mera")]
    [Tooltip("O GameObject que a c�mera seguir�.")]
    public Transform target;
    [Tooltip("Dist�ncia da c�mera em rela��o ao alvo.")]
    public float distance = 10f;
    [Tooltip("Altura da c�mera em rela��o ao alvo.")]
    public float height = 5f;
    [Tooltip("Velocidade de suaviza��o do movimento da c�mera.")]
    public float smoothSpeed = 0.125f;
    [Tooltip("Sensibilidade da rota��o horizontal do mouse.")]
    public float mouseSensitivityX = 100f;
    [Tooltip("Sensibilidade da rota��o vertical do mouse.")]
    public float mouseSensitivityY = 100f;
    [Tooltip("Limites da rota��o vertical da c�mera (evita invers�o).")]
    public Vector2 pitchLimits = new Vector2(-45f, 80f); // X = min, Y = max

    private BalloonInputActions _inputActions;
    private float _yaw = 0f; // Rota��o horizontal
    private float _pitch = 0f; // Rota��o vertical

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