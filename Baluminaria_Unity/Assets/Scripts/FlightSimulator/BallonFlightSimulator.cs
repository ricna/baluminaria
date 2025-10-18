using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class BalloonFlightController : MonoBehaviour
{
    [Header("Propriedades Físicas do Balão")]
    [Tooltip("Massa total do balão (cesta, envelope, piloto, carga) em kg.")]
    public float totalMass = 200f;
    [Tooltip("Volume do envelope do balão em metros cúbicos.")]
    public float envelopeVolume = 2500f;
    [Tooltip("Arrasto (drag) aerodinâmico do balão. Ajuste para realismo.")]
    public float dragCoefficient = 0.05f;
    [Tooltip("Arrasto angular (angular drag) para rotação.")]
    public float angularDrag = 1f;

    [Header("Sistema de Temperatura e Queimador")]
    public float initBalloonTemperatureCelsius = 44f;
    public float minBalloonTemperatureCelsius = 20f;
    public float maxBalloonTemperatureCelsius = 120f;
    public float heatingRatePerSecond = 10f;
    public float coolingRatePerSecond = 5f;
    public float selfCoolingRateForSecond = 0.5f;
    public float coolingRatePerVelocity = 0.01f;
    public Light burnerLight;
    public float maxBurnerLightIntensity = 5f;
    public float burnerLightFlickerFrequency = 10f;
    public float burnerLightFlickerAmplitude = 0.2f;

    [Header("Combustível")]
    public float maxFuel = 100f;
    public float fuelConsumptionRate = 1f;

    [Header("Condições Ambientais")]
    public float groundLevelTemperatureCelsius = 20f;
    public float temperatureLapseRate = 0.0065f;
    public float groundLevelPressurePascals = 101325f;
    public float R_air = 287.05f;

    [Header("Controle e Movimento")]
    [Tooltip("Velocidade de giro visual do balão (quanto menor, mais lento)")]
    public float turnSmoothSpeed = 0.5f; // quanto mais baixo, mais lento o giro visual
    [Tooltip("Magnitude desejada para 'forward thrust' na direção da câmera (mantém velocidade coerente).")]
    public float forwardThrust = 1.5f;   // força direcional (valor alvo de velocidade horizontal)
    [Tooltip("Suavização na transição de direção (0..1).")]
    public float smoothingFactor = 0.1f; // suavização da direção do movimento
    [Tooltip("Fator de mistura ao aplicar a nova velocidade (quanto mais baixo, mais suave).")]
    public float velocityBlend = 0.05f;
    [Tooltip("Suavização da desaceleração quando soltar WASD (0..1, quanto menor, mais suave).")]
    public float decelerationSmoothness = 0.02f;

    [Header("Vento Simulado")]
    public float windStrength = 0.5f;
    public float windTurbulenceFrequency = 0.1f;
    public float windTurbulenceAmplitude = 0.2f;

    [Header("UI Debug (TextMeshPro)")]
    public TextMeshProUGUI altitudeText;
    public TextMeshProUGUI verticalSpeedText;
    public TextMeshProUGUI internalTempText;
    public TextMeshProUGUI externalTempText;
    public TextMeshProUGUI fuelText;
    public TextMeshProUGUI modeText;

    // Componentes
    private Rigidbody _rb;
    private BalloonInputActions _inputActions;

    // Variáveis de Input
    private Vector2 _moveInput;
    private bool _burnerActive;
    private bool _coolerActive;

    // Estado do Balão
    public bool IsAutomaticMode { get; private set; } = false;
    public float CurrentBalloonTemperatureCelsius { get; private set; }
    public float CurrentFuel { get; private set; }
    public float CurrentExternalTemperatureCelsius { get; private set; }
    public float CurrentAltitude { get; private set; }

    // Referência para o CameraController (atribua pelo GameManager ou inspector)
    [HideInInspector]
    public CameraController cameraController;

    // Eventos
    public event System.Action<bool> OnAutomaticModeChanged;

    // Suavização de direção
    private Vector3 _targetDirection = Vector3.zero;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configurar Rigidbody
        _rb.mass = totalMass;
        _rb.useGravity = true;
        _rb.angularDamping = angularDrag;
        _rb.isKinematic = false;

        // Note: mantenho linear damping no campo dragCoefficient para reduzir oscilações horizontais
        _rb.linearDamping = dragCoefficient;

        // Inicializar sistema de input
        _inputActions = new BalloonInputActions();

        _inputActions.BalloonControls.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _inputActions.BalloonControls.Move.canceled += ctx => _moveInput = Vector2.zero;

        _inputActions.BalloonControls.Ascend.performed += ctx => _burnerActive = true;
        _inputActions.BalloonControls.Ascend.canceled += ctx => _burnerActive = false;

        _inputActions.BalloonControls.Descend.performed += ctx => { _coolerActive = true; };
        _inputActions.BalloonControls.Descend.canceled += ctx => { _coolerActive = false; };

        _inputActions.BalloonControls.ToggleAutomatic.performed += ctx => ToggleAutomaticMode();

        // Inicializar estados
        CurrentBalloonTemperatureCelsius = initBalloonTemperatureCelsius;
        CurrentFuel = maxFuel;

        if (burnerLight != null)
        {
            burnerLight.intensity = 0f;
            burnerLight.enabled = false;
        }
    }

    private void OnEnable()
    {
        _inputActions.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Disable();
    }

    private void FixedUpdate()
    {
        CurrentAltitude = transform.position.y;
        CalculateExternalConditions();
        UpdateBalloonTemperatureAndFuel();
        ApplyDynamicBuoyancy();
        ApplyWind();
        ApplyAutoStabilization();
        if (IsAutomaticMode)
        {
            // Automatic mode - AI externa deve controlar via ActivateBurner/ApplyAIForce/ApplyAITurn
        }
        else
        {
            HandleManualMovement();
            HandleVisualRotation();
        }

        UpdateBurnerLight();
        UpdateDebugUI();
    }

    private void CalculateExternalConditions()
    {
        CurrentExternalTemperatureCelsius = groundLevelTemperatureCelsius - (CurrentAltitude * temperatureLapseRate);
        CurrentExternalTemperatureCelsius = Mathf.Max(CurrentExternalTemperatureCelsius, -50f);
    }

    private void UpdateBalloonTemperatureAndFuel()
    {
        if (_burnerActive && CurrentFuel > 0 && !IsAutomaticMode)
        {
            CurrentBalloonTemperatureCelsius += heatingRatePerSecond * Time.fixedDeltaTime;
            CurrentFuel -= fuelConsumptionRate * Time.fixedDeltaTime;
            if (CurrentFuel < 0f)
            {
                CurrentFuel = 0f;
            }
        }
        else if (IsAutomaticMode)
        {
            // AI controla o queimador externamente
        }

        if (_coolerActive)
        {
            CurrentBalloonTemperatureCelsius -= coolingRatePerSecond * Time.fixedDeltaTime;
        }

        float coolingRate = selfCoolingRateForSecond + (_rb.linearVelocity.magnitude * coolingRatePerVelocity);
        CurrentBalloonTemperatureCelsius -= coolingRate * Time.fixedDeltaTime;

        CurrentBalloonTemperatureCelsius = Mathf.Clamp(CurrentBalloonTemperatureCelsius, minBalloonTemperatureCelsius, maxBalloonTemperatureCelsius);

        if (CurrentFuel <= 0f)
        {
            _burnerActive = false;
        }
    }

    private void ApplyDynamicBuoyancy()
    {
        float tempInternalKelvin = CurrentBalloonTemperatureCelsius + 273.15f;
        float tempExternalKelvin = CurrentExternalTemperatureCelsius + 273.15f;

        float currentPressurePascals = groundLevelPressurePascals;

        float densityAirInternal = currentPressurePascals / (R_air * tempInternalKelvin);
        float densityAirExternal = currentPressurePascals / (R_air * tempExternalKelvin);

        float gravitationalAcceleration = Physics.gravity.magnitude;
        float buoyancyForceMagnitude = envelopeVolume * (densityAirExternal - densityAirInternal) * gravitationalAcceleration;

        _rb.AddForce(Vector3.up * buoyancyForceMagnitude, ForceMode.Force);
    }

    private void ApplyWind()
    {
        float turbulenceX = Mathf.PerlinNoise(Time.time * windTurbulenceFrequency, 0f) * 2f - 1f;
        float turbulenceZ = Mathf.PerlinNoise(0f, Time.time * windTurbulenceFrequency) * 2f - 1f;

        Vector3 windDirection = new Vector3(turbulenceX, 0f, turbulenceZ).normalized;
        Vector3 windForce = windDirection * windStrength * (1f + windTurbulenceAmplitude * Mathf.Sin(Time.time));

        _rb.AddForce(windForce, ForceMode.Force);
    }

    private void HandleManualMovement()
    {
        if (cameraController == null)
        {
            return;
        }

        // Direção baseada na câmera (WASD direciona para onde a câmera aponta)
        Vector3 camForward = cameraController.transform.forward;
        Vector3 camRight = cameraController.transform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 desiredDirection = (camForward * _moveInput.y + camRight * _moveInput.x).normalized;

        // Atualiza target direction com suavização (não zera instantaneamente)
        _targetDirection = Vector3.Lerp(_targetDirection, desiredDirection, smoothingFactor);

        // Se houver input, redireciona a velocidade horizontal para a direção da câmera
        if (_targetDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 currentVelocity = _rb.linearVelocity;
            float verticalComponent = currentVelocity.y;

            // Target horizontal speed baseado em forwardThrust
            Vector3 desiredHorizontalVelocity = _targetDirection * forwardThrust;

            // Mantém componente vertical atual
            desiredHorizontalVelocity.y = verticalComponent;

            // Blend suave entre velocidade atual e desejada
            Vector3 blended = Vector3.Lerp(currentVelocity, desiredHorizontalVelocity, velocityBlend);

            // Aplica como nova velocidade
            _rb.linearVelocity = blended;
        }
        else
        {
            // Quando o jogador solta o WASD, desacelera suavemente a velocidade horizontal
            Vector3 currentVelocity = _rb.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

            // Reduz gradualmente a velocidade horizontal sem afetar o eixo Y
            horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, decelerationSmoothness);

            _rb.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);
        }
    }

    private void HandleVisualRotation()
    {
        if (cameraController == null)
        {
            return;
        }

        // Visual rotation: acompanha yaw da câmera lentamente, mas não muda a direção do movimento
        float targetYaw = cameraController.GetYaw();
        Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSmoothSpeed * Time.fixedDeltaTime);
    }

    private void UpdateBurnerLight()
    {
        if (burnerLight == null)
        {
            return;
        }

        if (_burnerActive && CurrentFuel > 0f)
        {
            burnerLight.enabled = true;
            float flicker = 1f + Mathf.PerlinNoise(Time.time * burnerLightFlickerFrequency, 0f) * burnerLightFlickerAmplitude;
            burnerLight.intensity = maxBurnerLightIntensity * flicker;
        }
        else
        {
            burnerLight.intensity = 0f;
            burnerLight.enabled = false;
        }
    }

    private void UpdateDebugUI()
    {
        if (altitudeText != null)
        {
            altitudeText.text = $"Altitude: {CurrentAltitude:F1} m";
        }

        if (verticalSpeedText != null)
        {
            verticalSpeedText.text = $"Vel. Vert.: {_rb.linearVelocity.y:F1} m/s";
        }

        if (internalTempText != null)
        {
            internalTempText.text = $"Temp Int: {CurrentBalloonTemperatureCelsius:F1} °C";
        }

        if (externalTempText != null)
        {
            externalTempText.text = $"Temp Ext: {CurrentExternalTemperatureCelsius:F1} °C";
        }

        if (fuelText != null)
        {
            fuelText.text = $"Combustível: {CurrentFuel:F1} L";
        }

        if (modeText != null)
        {
            modeText.text = $"Modo: {(IsAutomaticMode ? "AUTOMÁTICO" : "MANUAL")}";
        }
    }

    public void ToggleAutomaticMode()
    {
        IsAutomaticMode = !IsAutomaticMode;
        OnAutomaticModeChanged?.Invoke(IsAutomaticMode);

        if (IsAutomaticMode)
        {
            Debug.Log("Modo Automático Ativado.");
            _moveInput = Vector2.zero;
            _burnerActive = false;
        }
        else
        {
            Debug.Log("Modo Manual Ativado.");
        }
    }

    // Métodos públicos reintroduzidos — preservam compatibilidade com outros scripts/AI

    public void ActivateBurner(bool activate)
    {
        if (IsAutomaticMode)
        {
            _burnerActive = activate;
        }
    }

    public void ApplyAIForce(Vector3 force)
    {
        if (IsAutomaticMode)
        {
            _rb.AddForce(force, ForceMode.Acceleration);
        }
    }

    public void ApplyAITurn(float turnAmount)
    {
        if (IsAutomaticMode)
        {
            transform.Rotate(Vector3.up, turnAmount * Time.fixedDeltaTime);
        }
    }

    public float stabilizeStrength = 0.5f; // força de estabilização (ajuste conforme)
    public float stabilizeDamping = 0.2f;  // amortecimento da oscilação (0 = sem amortecimento)
    private void ApplyAutoStabilization()
    {
        // Calcula o quanto o balão está inclinado em X/Z
        Vector3 currentEuler = transform.rotation.eulerAngles;

        // Converte para intervalo [-180, 180]
        if (currentEuler.x > 180f) currentEuler.x -= 360f;
        if (currentEuler.z > 180f) currentEuler.z -= 360f;

        Vector3 correctiveTorque = new Vector3(-currentEuler.x * stabilizeStrength, 0f, -currentEuler.z * stabilizeStrength);

        // Aplica torque suavizado
        Vector3 dampedTorque = Vector3.Lerp(Vector3.zero, correctiveTorque, 1f - stabilizeDamping);

        _rb.AddTorque(dampedTorque, ForceMode.Acceleration);
    }


    public Rigidbody GetRigidbody()
    {
        return _rb;
    }
}
