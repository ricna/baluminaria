using UnityEngine;
using System.Collections.Generic;

public class BalloonAIController : MonoBehaviour
{
    [Header("Configurações do Piloto Automático")]
    [Tooltip("Velocidade desejada do balão no piloto automático.")]
    public float targetSpeed = 10f;
    [Tooltip("Altura desejada de voo no piloto automático.")]
    public float targetAltitude = 150f;
    [Tooltip("Margem de erro para a altura alvo antes de ajustar o queimador.")]
    public float altitudeTolerance = 5f;
    [Tooltip("Temperatura interna alvo para manter a altitude.")]
    public float targetInternalTemperature = 90f; // Exemplo de temperatura para manter o voo
    [Tooltip("Velocidade de rotação para o próximo waypoint.")]
    public float turnRate = 0.5f;
    [Tooltip("Distância mínima para considerar um waypoint alcançado.")]
    public float waypointReachDistance = 20f;
    [Tooltip("Força horizontal para manter o balão em movimento na direção do waypoint.")]
    public float aiForwardForce = 0.5f; // Força para AI para frente

    [Header("Waypoints para Piloto Automático")]
    public List<Vector3> waypoints = new List<Vector3>();

    private BaluminariaFlightController _flightController;
    private Rigidbody _balloonRigidbody;
    private int _currentWaypointIndex = 0;
    private bool _initializedWaypoints = false;

    private void Awake()
    {

    }

    private void Start()
    {
        _flightController = GetComponent<BaluminariaFlightController>();
        if (_flightController == null)
        {
            Debug.LogError("BalloonAIController requer um BalloonFlightController no mesmo GameObject!");
            enabled = false;
            return;
        }

        _balloonRigidbody = _flightController.GetRigidbody();
        if (_balloonRigidbody == null)
        {
            Debug.LogError("Rigidbody não encontrado no BalloonFlightController!");
            enabled = false;
            return;
        }

        _flightController.OnAutomaticModeChanged += OnAutomaticModeChanged;
        this.enabled = _flightController.IsAutomaticMode;
        InitializeWaypoints();
    }

    private void OnDestroy()
    {
        if (_flightController != null)
        {
            _flightController.OnAutomaticModeChanged -= OnAutomaticModeChanged;
        }
    }

    private void OnAutomaticModeChanged(bool isAutomatic)
    {
        this.enabled = isAutomatic;
        if (isAutomatic && !_initializedWaypoints)
        {
            InitializeWaypoints();
        }
        // Desliga o queimador se o modo automático for desativado
        if (!isAutomatic)
        {
            _flightController.ActivateBurner(false);
        }
    }

    private void FixedUpdate()
    {
        if (!_flightController.IsAutomaticMode || waypoints.Count == 0)
        {
            _flightController.ActivateBurner(false); // Garante que o queimador do AI esteja desligado
            return;
        }

        HandleWaypointFollowing();
        ControlAltitudeAndTemperature();
        MaintainSpeed();
    }

    private void InitializeWaypoints()
    {
        if (waypoints.Count == 0)
        {
            Debug.LogWarning("Nenhum waypoint definido. Gerando waypoints de exemplo.");
            for (int i = 0; i < 5; i++)
            {
                Vector3 randomPoint = transform.position + new Vector3(
                    Random.Range(-500f, 500f),
                    Random.Range(100f, 300f), // Waypoints com altitude variada para o AI
                    Random.Range(-500f, 500f)
                );
                waypoints.Add(randomPoint);
            }
        }
        _currentWaypointIndex = 0;
        _initializedWaypoints = true;
    }

    private void HandleWaypointFollowing()
    {
        if (waypoints.Count == 0) return;

        Vector3 targetWaypoint = waypoints[_currentWaypointIndex];
        // O AIController agora tenta se alinhar à altura do waypoint também.
        Vector3 directionToWaypoint = (targetWaypoint - transform.position).normalized;

        float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint);

        Quaternion targetRotation = Quaternion.LookRotation(directionToWaypoint);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnRate * Time.fixedDeltaTime);

        if (distanceToWaypoint < waypointReachDistance)
        {
            _currentWaypointIndex++;
            if (_currentWaypointIndex >= waypoints.Count)
            {
                Debug.Log("Todos os waypoints concluídos. Reiniciando...");
                _currentWaypointIndex = 0;
            }
            // Quando um waypoint é alcançado, atualiza a altura alvo para o próximo waypoint
            targetAltitude = waypoints[_currentWaypointIndex].y;
        }
    }

    private void ControlAltitudeAndTemperature()
    {
        // NOVO: O AI controla o queimador para atingir a altitude alvo
        float altitudeDifference = targetAltitude - _flightController.CurrentAltitude;

        if (altitudeDifference > altitudeTolerance) // Precisa subir
        {
            _flightController.ActivateBurner(true); // Liga o queimador
        }
        else if (altitudeDifference < -altitudeTolerance) // Precisa descer
        {
            _flightController.ActivateBurner(false); // Desliga o queimador para resfriar
            // Se o balão estiver muito alto e não descer rápido o suficiente,
            // poderíamos implementar uma "válvula de ar" aqui no futuro.
        }
        else // Está na altitude alvo, tenta manter a temperatura
        {
            // Tenta manter a temperatura interna em um nível que mantenha a flutuabilidade
            if (_flightController.CurrentBalloonTemperatureCelsius < targetInternalTemperature && _flightController.CurrentFuel > 0)
            {
                _flightController.ActivateBurner(true);
            }
            else if (_flightController.CurrentBalloonTemperatureCelsius > targetInternalTemperature)
            {
                _flightController.ActivateBurner(false);
            }
        }
    }

    private void MaintainSpeed()
    {
        Vector3 currentHorizontalVelocity = new Vector3(_balloonRigidbody.linearVelocity.x, 0, _balloonRigidbody.linearVelocity.z);
        float currentSpeed = currentHorizontalVelocity.magnitude;

        // Se estiver muito lento, aplica uma pequena força para frente
        if (currentSpeed < targetSpeed)
        {
            _flightController.ApplyAIForce(transform.forward * aiForwardForce);
        }
        // Se estiver muito rápido, não faz nada além do drag natural
        // Ou poderia aplicar um pequeno arrasto inverso
        else if (currentSpeed > targetSpeed)
        {
            _flightController.ApplyAIForce(-currentHorizontalVelocity.normalized * aiForwardForce * 0.5f);
        }
    }

    private void OnDrawGizmos()
    {
        if (waypoints.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Count; i++)
            {
                Gizmos.DrawWireSphere(waypoints[i], waypointReachDistance);
                if (i < waypoints.Count - 1)
                {
                    Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);
                }
            }
            if (_initializedWaypoints && _currentWaypointIndex < waypoints.Count)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(waypoints[_currentWaypointIndex], waypointReachDistance / 2);
            }
        }
    }
}