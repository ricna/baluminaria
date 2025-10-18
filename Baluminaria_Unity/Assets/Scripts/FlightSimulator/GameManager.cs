using TMPro; // Para UI de medidores
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public BalloonFlightController balloonFlightController;
    public CameraController cameraController;
    public Baluminaria baluminaria;
    /*
    [Header("Configura��es do Ambiente")]
    public Material skyboxMaterial;
    public Color manualSkyColor = Color.cyan;
    public Color automaticSkyColor = Color.blue;
    public Light directionalLight;
    */
    [Header("Configura��es de �udio")]
    public AudioSource windSoundSource;
    public AudioSource ambientMusicSource;
    public AudioSource burnerSoundSource; // NOVO: Adicione uma fonte de �udio para o queimador
    [SerializeField]
    private BalloonInputActions _inputActions;

    private void Start()
    {
        if (balloonFlightController == null)
        {
            Debug.LogError("balloonFlightController n�o atribu�do no GameManager!");
            return;
        }
        if (cameraController == null)
        {
            Debug.LogError("cameraController n�o atribu�do no GameManager!");
            return;
        }

        balloonFlightController.cameraController = cameraController;

        balloonFlightController.OnAutomaticModeChanged += OnAutomaticModeChanged;

        //SetSkyColors(balloonFlightController.IsAutomaticMode ? automaticSkyColor : manualSkyColor);
        UpdateAudioSettings();

        if (baluminaria != null)
        {
            baluminaria.SetAutoRotate(false);
        }
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
        //SetSkyColors(isAutomatic ? automaticSkyColor : manualSkyColor);
        UpdateAudioSettings();
    }

    private void Update() // Update � bom para controlar sons baseados no estado do queimador
    {
        // Se o queimador estiver ativo no FlightController, toque o som.
        // Assumindo que o burnerSoundSource � um loop
        if (balloonFlightController != null && balloonFlightController.CurrentFuel > 0 && _inputActions.BalloonControls.Ascend.IsPressed())
        {
            if (burnerSoundSource != null && !burnerSoundSource.isPlaying)
            {
                burnerSoundSource.Play();
            }
        }
        else
        {
            if (burnerSoundSource != null && burnerSoundSource.isPlaying)
            {
                burnerSoundSource.Stop();
            }
        }
    }


    public void SetSkyColors(Color newColor)
    {
        /*RenderSettings.skybox = skyboxMaterial;
        RenderSettings.ambientLight = newColor;

        if (directionalLight != null)
        {
            directionalLight.color = newColor * 0.8f;
        }*/
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