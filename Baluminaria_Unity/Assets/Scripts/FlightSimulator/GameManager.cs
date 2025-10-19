using MPTK.NAudio.Midi;
using System;
using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public BaluminariaFlightController balloonFlightController;
    public CameraController cameraController;
    public Baluminaria baluminaria;

    [Header("Configura��es de �udio")]
    public AudioSource windSoundSource;
    public AudioSource ambientMusicSource;
    public AudioSource burnerSoundSource;
    [SerializeField]
    private AudioClip _burnerStart;
    [SerializeField]
    private AudioClip _burnerLoop;
    [SerializeField]
    private AudioClip _burnerStop;
    private AudioSource _audioSourceBurner;

    private bool _isAscending = false;
    private bool _isDescending = false;


    [Header("Personaliza��o")]
    [SerializeField]
    private PersonalizationManager _personalizationManager;
    [SerializeField]
    private BaluMidiController _baluMidiController;

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

        UpdateAudioSettings();

        if (baluminaria != null)
        {
            baluminaria.SetAutoRotate(false); // bloqueia rota��o at� personaliza��o/confirm
        }

        AssignOnEvents();

        // Fluxo: checar se existe arquivo salvo; se n�o, iniciar personaliza��o.
        string path = Path.Combine(Application.persistentDataPath, "baluminaria_config.json");
        if (File.Exists(path))
        {
            Debug.Log("Design existente encontrado. Carregando e aplicando...");
            string json = File.ReadAllText(path);
            BaluminariaData loaded = JsonUtility.FromJson<BaluminariaData>(json);
            if (loaded != null)
            {
                ApplyDesignImmediately(loaded);
                // inicia modo de voo + MIDI automaticamente
                StartExperienceFromDesign(loaded);
            }
            else
            {
                Debug.LogWarning("Arquivo de design encontrado mas n�o p�de ser desserializado. Entrando em modo de personaliza��o.");
                StartPersonalizationFlow();
            }
        }
        else
        {
            Debug.Log("Nenhum design salvo encontrado. Iniciando personaliza��o.");
            StartPersonalizationFlow();
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


    private void AssignOnEvents()
    {
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
    // --------------------------------------------------------
    // Personalization / Experience Flow
    // --------------------------------------------------------
    private void StartPersonalizationFlow()
    {
        if (_personalizationManager != null)
        {
            _personalizationManager.StartPersonalization();
        }
        else
        {
            Debug.LogError("PersonalizationManager n�o atribu�do no GameManager.");
        }
    }

    public void StartExperienceFromDesign(BaluminariaData design)
    {
        if (design == null)
        {
            Debug.LogError("Design nulo passado para StartExperienceFromDesign.");
            return;
        }

        // Aplica design na baluminaria (visual imediato)
        ApplyDesignImmediately(design);

        // Inicia o flight controller (modo de voo)
        if (balloonFlightController != null)
        {
            balloonFlightController.StartFlying();
        }

        // Inicia o MIDI controller
        if (_baluMidiController != null)
        {
            _baluMidiController.SetOutputMode(BaluMidiController.OutputMode.Baluminaria);
            _baluMidiController.StartMidiController();
        }

        // Ativa rota��o autom�tica se desejar
        if (baluminaria != null)
        {
            baluminaria.SetAutoRotate(true);
        }
    }

    private void ApplyDesignImmediately(BaluminariaData design)
    {
        if (design == null || baluminaria == null) return;

        Segment[] segments = baluminaria.GetSegments();
        int length = Mathf.Min(segments.Length, design.segmentColors.Length);
        for (int i = 0; i < length; i++)
        {
            if (segments[i] != null && design.segmentColors[i] != null)
            {
                Color color = design.segmentColors[i].ToColor();
                segments[i].SetMaterialColors(color, color);
                segments[i].ChangeLightColor(color);
                segments[i].SetLightIntensity(0f);
            }
        }
    }
}
