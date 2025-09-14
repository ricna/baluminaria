using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RecordingManager : MonoBehaviour
{
    // A UI RawImage para mostrar o feed da sua webcam.
    [Header("Elementos da UI")]
    [Tooltip("Arraste aqui a RawImage que exibir� o feed da c�mera.")]
    public RawImage cameraDisplay;
    public Button _btnCapture;
    [Tooltip("Um campo de texto para mostrar o status da captura.")]
    public TMP_Text statusText;

    // --- Configura��es de Captura ---
    // O nome do dispositivo de microfone que ser� usado.
    private string selectedMicrophone;
    // O nome do dispositivo de c�mera que ser� usado.
    private string selectedWebcam;

    // A textura que receber� o feed da webcam.
    private WebCamTexture webcamTexture;
    // O AudioSource que receber� o �udio do microfone. O Unity Recorder o usar� como fonte.
    private AudioSource audioSource;

    private void Start()
    {
        // Garante que os bot�es est�o configurados
        if (_btnCapture != null)
        {
            _btnCapture.onClick.AddListener(SwitchCapture);
        }
        // Obt�m o componente AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Inicializa a UI
        UpdateStatus("Pronto para capturar �udio e v�deo.");
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
    // Fun��o principal para iniciar a captura
    private void StartCapture()
    {
        // 1. Captura de �udio do Piano Digital
        // Pega o primeiro microfone dispon�vel. Voc� pode ajustar isso se tiver mais de um.
        if (Microphone.devices.Length > 0)
        {
            selectedMicrophone = Microphone.devices[0];
            Debug.Log($"Iniciando captura de �udio com o dispositivo: {selectedMicrophone}");

            // O Unity Recorder precisa de um AudioSource para gravar.
            // O Microphone.Start alimenta o sinal de �udio diretamente neste AudioSource.
            audioSource.clip = Microphone.Start(selectedMicrophone, true, 300, AudioSettings.outputSampleRate);
            audioSource.loop = true;

            // Espera o clipe de �udio carregar antes de come�ar a tocar.
            // O sinal do microfone n�o fica imediatamente dispon�vel.
            while (!(Microphone.GetPosition(selectedMicrophone) > 0)) { }
            audioSource.Play();
        }
        else
        {
            Debug.LogError("Nenhum microfone encontrado. Verifique suas conex�es.");
            UpdateStatus("ERRO: Nenhum microfone encontrado.");
        }

        // 2. Captura de V�deo da Webcam
        // Pega a primeira c�mera dispon�vel.
        if (WebCamTexture.devices.Length > 0)
        {
            selectedWebcam = WebCamTexture.devices[0].name;
            Debug.Log($"Iniciando captura de v�deo com o dispositivo: {selectedWebcam}");

            webcamTexture = new WebCamTexture(selectedWebcam);
            cameraDisplay.texture = webcamTexture;
            cameraDisplay.material.mainTexture = webcamTexture;

            // Inicia o feed da c�mera
            webcamTexture.Play();
        }
        else
        {
            Debug.LogError("Nenhuma webcam encontrada. Verifique suas conex�es.");
            UpdateStatus("ERRO: Nenhuma webcam encontrada.");
        }

        // Atualiza a UI para o estado de grava��o
        UpdateStatus("Capturando feeds. Pressione 'Iniciar Grava��o' no Unity Recorder.");
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

        UpdateStatus("Captura interrompida. Pronto para come�ar novamente.");
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
        // Garante que a c�mera e o microfone s�o desligados ao fechar o aplicativo.
        StopCapture();
    }
}
