using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecordingManager : MonoBehaviour
{
    // A UI RawImage para mostrar o feed da sua webcam.
    [Header("Elementos da UI")]
    [Tooltip("Arraste aqui a RawImage que exibirá o feed da câmera.")]
    public RawImage cameraDisplay;
    public Button _btnCapture;
    [Tooltip("Um campo de texto para mostrar o status da captura.")]
    public TMP_Text statusText;

    // --- Configurações de Captura ---
    // O nome do dispositivo de microfone que será usado.
    private string selectedMicrophone;
    // O nome do dispositivo de câmera que será usado.
    private string selectedWebcam;

    // A textura que receberá o feed da webcam.
    private WebCamTexture webcamTexture;
    // O AudioSource que receberá o áudio do microfone. O Unity Recorder o usará como fonte.
    private AudioSource audioSource;

    private void Start()
    {
        // Garante que os botões estão configurados
        if (_btnCapture != null)
        {
            _btnCapture.onClick.AddListener(SwitchCapture);
        }
        // Obtém o componente AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Inicializa a UI
        UpdateStatus("Pronto para capturar áudio e vídeo.");
    }
    private void SwitchCapture()
    {
        if (_capturing)
        {
            StopCapture();
            _capturing = false;
        }
        else
        {
            StartCapture();
            _capturing = true;
        }
    }
    [SerializeField]
    private bool _capturing = false;
    // Função principal para iniciar a captura
    private void StartCapture()
    {
        // 1. Captura de Áudio do Piano Digital
        // Pega o primeiro microfone disponível. Você pode ajustar isso se tiver mais de um.
        if (Microphone.devices.Length > 0)
        {
            selectedMicrophone = Microphone.devices[0];
            Debug.Log($"Iniciando captura de áudio com o dispositivo: {selectedMicrophone}");

            // O Unity Recorder precisa de um AudioSource para gravar.
            // O Microphone.Start alimenta o sinal de áudio diretamente neste AudioSource.
            audioSource.clip = Microphone.Start(selectedMicrophone, true, 300, AudioSettings.outputSampleRate);
            audioSource.loop = true;

            // Espera o clipe de áudio carregar antes de começar a tocar.
            // O sinal do microfone não fica imediatamente disponível.
            while (!(Microphone.GetPosition(selectedMicrophone) > 0)) { }
            audioSource.Play();
        }
        else
        {
            Debug.LogError("Nenhum microfone encontrado. Verifique suas conexões.");
            UpdateStatus("ERRO: Nenhum microfone encontrado.");
        }

        // 2. Captura de Vídeo da Webcam
        // Pega a primeira câmera disponível.
        if (WebCamTexture.devices.Length > 0)
        {
            selectedWebcam = WebCamTexture.devices[0].name;
            Debug.Log($"Iniciando captura de vídeo com o dispositivo: {selectedWebcam}");

            webcamTexture = new WebCamTexture(selectedWebcam);
            cameraDisplay.texture = webcamTexture;
            cameraDisplay.material.mainTexture = webcamTexture;

            // Inicia o feed da câmera
            webcamTexture.Play();
        }
        else
        {
            Debug.LogError("Nenhuma webcam encontrada. Verifique suas conexões.");
            UpdateStatus("ERRO: Nenhuma webcam encontrada.");
        }

        // Atualiza a UI para o estado de gravação
        UpdateStatus("Capturando feeds. Pressione 'Iniciar Gravação' no Unity Recorder.");
        _btnCapture.colors = new ColorBlock
        {
            normalColor = colorDuringCapture,
            highlightedColor = colorDuringCapture,
            pressedColor = colorDuringCapture,
            selectedColor = colorDuringCapture,
            disabledColor = colorDuringCapture,
            colorMultiplier = 1,
            fadeDuration = 0.1f
        };
    }

    
    private Color colorDuringCapture = new Color(0.1f, 0.8f, 0.1f); // Verde
    private Color colorAfterStop = new Color(0.8f, 0.1f, 0.1f); // Vermelho

    private void StopCapture()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }

        if (audioSource != null && Microphone.IsRecording(selectedMicrophone))
        {
            Microphone.End(selectedMicrophone);
        }

        UpdateStatus("Captura interrompida. Pronto para começar novamente.");
        _btnCapture.colors = new ColorBlock
        {
            normalColor = colorAfterStop,
            highlightedColor = colorAfterStop,
            pressedColor = colorAfterStop,
            selectedColor = colorAfterStop,
            disabledColor = colorAfterStop,
            colorMultiplier = 1,
            fadeDuration = 0.1f
        };
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void OnApplicationQuit()
    {
        // Garante que a câmera e o microfone são desligados ao fechar o aplicativo.
        StopCapture();
    }
}
