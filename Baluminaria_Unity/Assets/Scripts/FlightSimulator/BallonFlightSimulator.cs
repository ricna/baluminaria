using UnityEngine;
using UnityEngine.InputSystem; // Importe o namespace do New Input System
using TMPro; // Para UI de medidores (se for usar TextMeshPro)

public class BalloonFlightController : MonoBehaviour
{
    [Header("Propriedades Físicas do Balão")]
    [Tooltip("Massa total do balão (cesta, envelope, piloto, carga) em kg.")]
    public float totalMass = 200f; // Massa total, incluindo carga, tripulantes, etc.
    [Tooltip("Volume do envelope do balão em metros cúbicos.")]
    public float envelopeVolume = 2500f; // Volume típico de um balão de tamanho médio
    [Tooltip("Arrasto (drag) aerodinâmico do balão. Ajuste para realismo.")]
    public float dragCoefficient = 0.05f; // Coeficiente de arrasto (ajuste fino)
    [Tooltip("Arrasto angular (angular drag) para rotação.")]
    public float angularDrag = 1f;

    [Header("Sistema de Temperatura e Queimador")]
    [Tooltip("Temperatura mínima interna do balão (temperatura ambiente aproximada).")]
    public float minBalloonTemperatureCelsius = 20f;
    [Tooltip("Temperatura máxima que o balão pode atingir.")]
    public float maxBalloonTemperatureCelsius = 120f;
    [Tooltip("Taxa de aquecimento do ar dentro do balão por segundo com o queimador ligado.")]
    public float heatingRatePerSecond = 10f; // Graus Celsius por segundo
    [Tooltip("Taxa de resfriamento do ar dentro do balão por segundo (passiva).")]
    public float coolingRatePerSecond = 0.5f; // Graus Celsius por segundo
    [Tooltip("Taxa adicional de resfriamento com o balão em movimento.")]
    public float coolingRatePerVelocity = 0.01f;
    [Tooltip("Luz que representa o queimador dentro do balão.")]
    public Light burnerLight;
    [Tooltip("Intensidade máxima da luz do queimador.")]
    public float maxBurnerLightIntensity = 5f;
    [Tooltip("Frequência de oscilação da luz do queimador.")]
    public float burnerLightFlickerFrequency = 10f;
    [Tooltip("Amplitude de oscilação da luz do queimador.")]
    public float burnerLightFlickerAmplitude = 0.2f;

    [Header("Combustível")]
    [Tooltip("Capacidade máxima de combustível.")]
    public float maxFuel = 100f;
    [Tooltip("Taxa de consumo de combustível por segundo quando o queimador está ativo.")]
    public float fuelConsumptionRate = 1f; // Unidades por segundo

    [Header("Condições Ambientais")]
    [Tooltip("Temperatura ambiente de referência no nível do mar em Celsius.")]
    public float groundLevelTemperatureCelsius = 20f;
    [Tooltip("Taxa de variação da temperatura ambiente com a altitude (graus por metro).")]
    public float temperatureLapseRate = 0.0065f; // ~6.5 C por 1000m
    [Tooltip("Pressão atmosférica de referência no nível do mar em Pascais.")]
    public float groundLevelPressurePascals = 101325f;
    [Tooltip("Constante dos gases ideais para o ar (J/(kg*K)).")]
    public float R_air = 287.05f; // Constante de ar seco

    [Header("Controle Manual")]
    [Tooltip("Velocidade de rotação horizontal do balão para seguir a câmera.")]
    public float turnSpeed = 2f;
    [Tooltip("Força horizontal de vento simulado ou micro-propulsão para frente.")]
    public float forwardThrust = 0.5f; // Leve, para ajustes finos

    [Header("Configurações de Vento (Simulado)")]
    [Tooltip("Intensidade base do vento horizontal.")]
    public float windStrength = 0.5f;
    [Tooltip("Frequência da variação do vento.")]
    public float windTurbulenceFrequency = 0.1f;
    [Tooltip("Amplitude da variação do vento.")]
    public float windTurbulenceAmplitude = 0.2f;

    // UI Debug Info (para TextMeshPro ou debug console)
    [Header("Debug UI (TextMeshPro)")]
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
    private bool _burnerActive; // Agora é um bool para o queimador

    // Estado do Balão
    public bool IsAutomaticMode { get; private set; } = false;
    public float CurrentBalloonTemperatureCelsius { get; private set; } // Temperatura do ar dentro do balão
    public float CurrentFuel { get; private set; }
    public float CurrentExternalTemperatureCelsius { get; private set; } // Temperatura ambiente
    public float CurrentAltitude { get; private set; } // Cache da altitude para cálculos

    // NOVO: Referência para o CameraController (será atribuída pelo GameManager)
    [HideInInspector]
    public CameraController cameraController;

    // Eventos
    public event System.Action<bool> OnAutomaticModeChanged;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        // Configurar Rigidbody
        _rb.mass = totalMass; // Use totalMass
        _rb.useGravity = true;
        _rb.linearDamping = dragCoefficient; // Use dragCoefficient
        _rb.angularDamping = angularDrag;
        _rb.isKinematic = false;

        // Inicializar sistema de input
        _inputActions = new BalloonInputActions();

        _inputActions.BalloonControls.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _inputActions.BalloonControls.Move.canceled += ctx => _moveInput = Vector2.zero;

        // Ascend agora controla o queimador
        _inputActions.BalloonControls.Ascend.performed += ctx => _burnerActive = true;
        _inputActions.BalloonControls.Ascend.canceled += ctx => _burnerActive = false;

        // Descend agora é para liberar ar quente (ou apenas deixar esfriar passivamente)
        // Para balões realistas, não há "descida forçada" exceto liberando ar ou o ar esfriando.
        // Podemos adicionar uma válvula de ar para descida rápida se desejar.
        // Por enquanto, apenas desliga o queimador para descer.
        _inputActions.BalloonControls.Descend.performed += ctx => { /* Implementar válvula de ar se necessário */ };
        _inputActions.BalloonControls.Descend.canceled += ctx => { /* ... */ };


        _inputActions.BalloonControls.ToggleAutomatic.performed += ctx => ToggleAutomaticMode();

        // Inicializar estados
        CurrentBalloonTemperatureCelsius = minBalloonTemperatureCelsius;
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

        if (IsAutomaticMode)
        {
            // O controle automático será gerenciado por BalloonAIController
            // E o BalloonAIController pode ativar o queimador ou não.
        }
        else
        {
            HandleManualMovement();
        }

        UpdateBurnerLight();
        UpdateDebugUI();
    }

    private void CalculateExternalConditions()
    {
        // Temperatura externa diminui com a altitude
        CurrentExternalTemperatureCelsius = groundLevelTemperatureCelsius - (CurrentAltitude * temperatureLapseRate);
        // Garante que não vá abaixo de um limite razoável (ex: -50C)
        CurrentExternalTemperatureCelsius = Mathf.Max(CurrentExternalTemperatureCelsius, -50f);
    }

    private void UpdateBalloonTemperatureAndFuel()
    {
        if (_burnerActive && CurrentFuel > 0 && !IsAutomaticMode) // Queimador manual ativo
        {
            CurrentBalloonTemperatureCelsius += heatingRatePerSecond * Time.fixedDeltaTime;
            CurrentFuel -= fuelConsumptionRate * Time.fixedDeltaTime;
            CurrentFuel = Mathf.Max(CurrentFuel, 0); // Garante que não seja negativo
        }
        else if (IsAutomaticMode)
        {
            // O AIController é responsável por ligar/desligar o queimador
        }

        // Resfriamento passivo
        float coolingRate = coolingRatePerSecond + (_rb.linearVelocity.magnitude * coolingRatePerVelocity);
        CurrentBalloonTemperatureCelsius -= coolingRate * Time.fixedDeltaTime;

        // Limita a temperatura interna
        CurrentBalloonTemperatureCelsius = Mathf.Clamp(CurrentBalloonTemperatureCelsius, minBalloonTemperatureCelsius, maxBalloonTemperatureCelsius);

        // Se o combustível acabar, o queimador desliga
        if (CurrentFuel <= 0)
        {
            _burnerActive = false; // Desliga o queimador
        }
    }

    private void ApplyDynamicBuoyancy()
    {
        // Converter temperaturas para Kelvin
        float tempInternalKelvin = CurrentBalloonTemperatureCelsius + 273.15f;
        float tempExternalKelvin = CurrentExternalTemperatureCelsius + 273.15f;

        // Assumimos que a pressão dentro e fora do balão é aproximadamente a mesma na altitude atual.
        // Poderíamos calcular a pressão com a altitude, mas para simplicidade inicial:
        // Pressão em uma dada altitude (simplificado da fórmula barométrica, pode ser mais complexo)
        // Aproximação simples: pressão diminui linearmente com a altura, ou usar uma função exponencial mais precisa.
        // Usaremos a pressão ao nível do mar como base e ajustaremos se necessário.
        float currentPressurePascals = groundLevelPressurePascals; // Simplificação, para simulação mais precisa precisaria de uma função de altura.

        // Calcular densidade do ar interno (balão) e externo (ambiente) usando a Lei dos Gases Ideais
        // rho = P / (R * T)
        float densityAirInternal = currentPressurePascals / (R_air * tempInternalKelvin);
        float densityAirExternal = currentPressurePascals / (R_air * tempExternalKelvin);

        // Força de flutuabilidade (princípio de Arquimedes):
        // F_buoyancy = Volume * (rho_external - rho_internal) * g
        float gravitationalAcceleration = Physics.gravity.magnitude;
        float buoyancyForceMagnitude = envelopeVolume * (densityAirExternal - densityAirInternal) * gravitationalAcceleration;

        // Aplicar a força
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
        // Rotação Horizontal: O balão segue a rotação Y da câmera para um controle de 3ª pessoa
        if (cameraController != null)
        {
            Quaternion targetYRotation = Quaternion.Euler(0, cameraController.GetYaw(), 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetYRotation, turnSpeed * Time.fixedDeltaTime);
        }

        // Movimento para frente/trás (na direção atual do balão)
        if (_moveInput.y != 0)
        {
            _rb.AddForce(transform.forward * forwardThrust * _moveInput.y, ForceMode.Acceleration);
        }
    }

    private void UpdateBurnerLight()
    {
        if (burnerLight == null) return;

        if (_burnerActive && CurrentFuel > 0)
        {
            burnerLight.enabled = true;
            // Oscila a intensidade da luz para simular a chama
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
        if (altitudeText != null) altitudeText.text = $"Altitude: {CurrentAltitude:F1} m";
        if (verticalSpeedText != null) verticalSpeedText.text = $"Vel. Vert.: {_rb.linearVelocity.y:F1} m/s";
        if (internalTempText != null) internalTempText.text = $"Temp Int: {CurrentBalloonTemperatureCelsius:F1} °C";
        if (externalTempText != null) externalTempText.text = $"Temp Ext: {CurrentExternalTemperatureCelsius:F1} °C";
        if (fuelText != null) fuelText.text = $"Combustível: {CurrentFuel:F1} L";
        if (modeText != null) modeText.text = $"Modo: {(IsAutomaticMode ? "AUTOMÁTICO" : "MANUAL")}";
    }

    public void ToggleAutomaticMode()
    {
        IsAutomaticMode = !IsAutomaticMode;
        OnAutomaticModeChanged?.Invoke(IsAutomaticMode);

        if (IsAutomaticMode)
        {
            Debug.Log("Modo Automático Ativado.");
            // Resetar inputs manuais ao entrar no modo automático
            _moveInput = Vector2.zero;
            _burnerActive = false; // O AIController assumirá o controle do queimador
        }
        else
        {
            Debug.Log("Modo Manual Ativado.");
        }
    }

    // Métodos para o AIController controlar
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

    public Rigidbody GetRigidbody()
    {
        return _rb;
    }
}