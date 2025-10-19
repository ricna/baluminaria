using TMPro; // Para UI de medidores
using UnityEngine;
using Unrez.BackyardShowdown;

public class GameManager : MonoBehaviour
{
    public BalloonFlightController balloonFlightController;
    public CameraController cameraController;
    public Baluminaria baluminaria;

    [Header("Configurações de Áudio")]
    public AudioSource windSoundSource;
    public AudioSource ambientMusicSource;
    public AudioSource burnerSoundSource; // NOVO: Adicione uma fonte de áudio para o queimador
    [SerializeField]
    private BalloonInputActions _inputActions;

    private void Start()
    {
        _inputActions = new BalloonInputActions();
        if (balloonFlightController == null)
        {
            Debug.LogError("balloonFlightController não atribuído no GameManager!");
            return;
        }
        if (cameraController == null)
        {
            Debug.LogError("cameraController não atribuído no GameManager!");
            return;
        }

        balloonFlightController.cameraController = cameraController;
        balloonFlightController.OnAutomaticModeChanged += OnAutomaticModeChanged;

        UpdateAudioSettings();

        if (baluminaria != null)
        {
            baluminaria.SetAutoRotate(false);
        }

        AssignOnEvents();
    }

    private bool _isAscending = false;
    private bool _isDescending = false;

    [SerializeField]
    private AudioClip _burnerStart;
    [SerializeField]
    private AudioClip _burnerLoop;
    [SerializeField]
    private AudioClip _burnerStop;
    private AudioSource _audioSourceBurner;
    private void AssignOnEvents()
    {
        _inputActions.BalloonControls.Enable();

        baluminaria.InputReader.OnAscendEvent += (isPressed) =>
        {
            if (isPressed && !_isAscending)
            {
                _audioSourceBurner = AudioManager.Instance.PlaySFXStartThenLoop(_burnerStart, _burnerLoop);
            }
            else if (!isPressed && _isAscending)
            {
                AudioManager.Instance.PlayOnceAfterLoop(_audioSourceBurner);
                AudioManager.Instance.PlaySFX(_burnerStop);
            }
            _isAscending = isPressed;
        };
        baluminaria.InputReader.OnDescendEvent += (isPressed) =>
        {
            _isDescending = isPressed;
        };
    }


    private void OnDestroy()
    {
        if (balloonFlightController != null)
        {
            balloonFlightController.OnAutomaticModeChanged -= OnAutomaticModeChanged;
        }
    }

    private void OnAutomaticModeChanged(bool isAutomatic)
    {
        Debug.Log($"GameMode Changed: {(isAutomatic ? "Automatic" : "Manual")}");
        UpdateAudioSettings();
    }

    private void UpdateAudioSettings()
    {
        if (balloonFlightController.IsAutomaticMode)
        {
            if (windSoundSource != null) windSoundSource.volume = 0.5f;
            if (ambientMusicSource != null && !ambientMusicSource.isPlaying) ambientMusicSource.Play();
        }
        else
        {
            if (windSoundSource != null) windSoundSource.volume = 1.0f;
            if (ambientMusicSource != null && ambientMusicSource.isPlaying) ambientMusicSource.Stop();
        }
    }
}