using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PersonalizationManager : MonoBehaviour
{
    public static PersonalizationManager Instance { get; private set; }

    [Header("Referências Principais")]
    [SerializeField] private Baluminaria _baluminaria;
    [SerializeField] private BaluMidiController _midiController;
    [SerializeField] private BaluminariaFlightController _flightController;
    [SerializeField] private GameManager _gameManager;

    [Header("Ambiente")]
    [SerializeField] private LightingPreviewManager _lightingPreviewManager;
    [SerializeField] private bool _enableDarkModeOption = true;


    [Header("UI e Interação")]
    [SerializeField] private GameObject _personalizationUI;
    [SerializeField] private GameObject _filamentPaletteParent; // Parent contendo os FilamentColorReference
    [SerializeField] private UnityEvent _onPersonalizationStart;
    [SerializeField] private UnityEvent _onPersonalizationConfirmed;
    [Header("Presets")]
    [SerializeField] private TMP_Dropdown _presetDropdown;  // ou TMP_Dropdown
    [SerializeField] private Button _loadPresetButton;


    [SerializeField] private Image _selectedColorPreview;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Color[] _presetPalette = new Color[8]; // pequenas paletas rápidas

    [Header("Settings")]
    [SerializeField] private string _saveFileName = "baluminaria_config.json";

    private Color _selectedColor = Color.white;
    private BaluminariaData _currentDesign;
    private FilamentColorReference _currentFilament;
    private bool _isEditing = false;

    // Eventos públicos para outras classes (ex: para mostrar analytics ou anim)
    public UnityEvent OnPersonalizationStarted;
    public UnityEvent OnPersonalizationConfirmed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // esconder UI por padrão
        if (_personalizationUI != null)
        {
            _personalizationUI.SetActive(false);
        }

        if (_filamentPaletteParent != null)
        {
            _filamentPaletteParent.SetActive(false);
        }

        // ligar callbacks
        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
        if (_saveButton != null)
        {
            _saveButton.onClick.AddListener(OnSaveButtonClicked);
        }
        if (_loadButton != null)
        {
            _loadButton.onClick.AddListener(OnLoadButtonClicked);
        }

        // Carrega a lista de presets no dropdown
        if (_presetDropdown != null)
        {
            List<string> presetNames = PresetLibrary.GetAvailablePresetNames();
            _presetDropdown.ClearOptions();
            _presetDropdown.AddOptions(presetNames);
        }

        if (_loadPresetButton != null)
        {
            _loadPresetButton.onClick.AddListener(OnLoadPresetClicked);
        }
        // inicializar paleta se estiver vazia
        if (_presetPalette.Length == 0)
        {
            _presetPalette = new Color[]
            {
                Color.white, Color.red, Color.magenta, Color.yellow, Color.cyan, Color.green, Color.gray, Color.black
            };
        }
        if (_filamentPaletteParent != null)
        {
            _filamentPaletteParent.SetActive(false);
        }
        _selectedColor = Color.white;
        UpdateSelectedColorPreview();
    }

    // PUBLIC API ---------------------------------------------------------
    public void StartPersonalization()
    {
        if (_isEditing) return;

        _isEditing = true;

        // cria design inicial baseado no estado atual da Baluminaria (se houver)
        Segment[] segments = _baluminaria.GetSegments();
        BaluminariaData design = new BaluminariaData();
        design.nomeBaluminaria = "MeuBalão";
        for (int i = 0; i < design.segmentColors.Length; i++)
        {
            if (i < segments.Length && segments[i] != null)
            {
                Color color = segments[i].CurrentBaseColor;
                design.segmentColors[i] = ColorData.FromColor(color);
            }
            else
            {
                design.segmentColors[i] = ColorData.FromColor(Color.black);
            }
        }

        _currentDesign = design;

        if (_personalizationUI != null) _personalizationUI.SetActive(true);

        if (_enableDarkModeOption && _lightingPreviewManager != null)
        {
            _lightingPreviewManager.SetDarkMode(true);
        }


        OnPersonalizationStarted?.Invoke();
    }

    public void StopPersonalization()
    {
        _isEditing = false;
        if (_enableDarkModeOption && _lightingPreviewManager != null)
        {
            _lightingPreviewManager.SetDarkMode(false);
        }
        if (_personalizationUI != null) _personalizationUI.SetActive(false);
    }

    public void SetSelectedFilament(FilamentColorReference filament)
    {
        _currentFilament = filament;
        Debug.Log($"Filamento selecionado: {filament.FilamentName}");
    }

    private int GetSegmentIndex(Segment segment)
    {
        Segment[] all = _baluminaria.GetSegments();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == segment) return i;
        }
        return -1;
    }


    public void OnSegmentClicked(Segment segment)
    {
        if (!_isEditing || segment == null) return;

        // Aplica cor selecionada ao segmento clicado e atualiza _currentDesign
        segment.SetMaterialColors(_selectedColor, _selectedColor);
        segment.ChangeLightColor(_selectedColor);

        // Atualiza o array serializável
        int index = GetSegmentIndex(segment);
        if (index >= 0 && _currentDesign != null && index < _currentDesign.segmentColors.Length)
        {
            _currentDesign.segmentColors[index] = ColorData.FromColor(_selectedColor);
        }
    }

    public void SelectPresetColor(int presetIndex)
    {
        if (presetIndex < 0 || presetIndex >= _presetPalette.Length) return;
        _selectedColor = _presetPalette[presetIndex];
        UpdateSelectedColorPreview();
    }

    public void SetSelectedColor(Color color)
    {
        _selectedColor = color;
        UpdateSelectedColorPreview();
    }

    // Buttons
    private void OnConfirmButtonClicked()
    {
        SaveCurrentDesignToDisk();
        ConfirmAndStartFlight();
    }

    private void OnCancelButtonClicked()
    {
        // reverte para estado anterior ao entrar em edição
        ReloadDesignFromCurrentDesignObject();
        StopPersonalization();
    }

    private void OnSaveButtonClicked()
    {
        SaveCurrentDesignToDisk();
    }

    private void OnLoadButtonClicked()
    {
        LoadDesignFromDiskAndApply();
    }
    private void OnLoadPresetClicked()
    {
        if (_presetDropdown == null) return;
        string selectedName = _presetDropdown.options[_presetDropdown.value].text;
        BaluminariaData preset = PresetLibrary.LoadPreset(selectedName);

        if (preset != null)
        {
            ApplyPreset(preset);
            Debug.Log($"Preset '{selectedName}' aplicado com sucesso!");
        }
        else
        {
            Debug.LogWarning($"Falha ao aplicar preset '{selectedName}'");
        }
    }

    private void ApplyPreset(BaluminariaData preset)
    {
        if (preset == null || _baluminaria == null) return;

        Segment[] segments = _baluminaria.GetSegments();
        int count = Mathf.Min(segments.Length, preset.segmentColors.Length);
        for (int i = 0; i < count; i++)
        {
            if (segments[i] != null)
            {
                Color c = preset.segmentColors[i].ToColor();
                segments[i].SetMaterialColors(c, c);
                segments[i].ChangeLightColor(c);
                segments[i].SetLightIntensity(0f);
            }
        }

        _currentDesign = preset;
    }


    private void UpdateSelectedColorPreview()
    {
        if (_selectedColorPreview != null)
        {
            _selectedColorPreview.color = _selectedColor;
        }
    }

    private string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, _saveFileName);
    }

    private void SaveCurrentDesignToDisk()
    {
        if (_currentDesign == null) return;

        _currentDesign.timestamp = DateTime.UtcNow.ToString("o");
        string json = JsonUtility.ToJson(_currentDesign, true);
        string path = GetSaveFilePath();
        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"Baluminaria design salvo em {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao salvar baluminaria design: {e.Message}");
        }
    }

    private BaluminariaData LoadDesignFromDiskInternal()
    {
        string path = GetSaveFilePath();
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                BaluminariaData loaded = JsonUtility.FromJson<BaluminariaData>(json);
                return loaded;
            }
            catch (Exception e)
            {
                Debug.LogError($"Erro lendo arquivo de design: {e.Message}");
            }
        }
        return null;
    }

    private void LoadDesignFromDiskAndApply()
    {
        BaluminariaData loaded = LoadDesignFromDiskInternal();
        if (loaded != null)
        {
            _currentDesign = loaded;
            ApplyDesignToBaluminaria(_currentDesign);
            Debug.Log("Design carregado e aplicado.");
        }
        else
        {
            Debug.Log("Nenhum design encontrado para carregar.");
        }
    }

    private void ReloadDesignFromCurrentDesignObject()
    {
        if (_currentDesign != null)
        {
            ApplyDesignToBaluminaria(_currentDesign);
        }
    }

    private void ApplyDesignToBaluminaria(BaluminariaData design)
    {
        if (design == null) return;
        Segment[] segments = _baluminaria.GetSegments();
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

    private void ConfirmAndStartFlight()
    {
        // garante que o design foi salvo
        SaveCurrentDesignToDisk();

        // dispara o fluxo de experiência imersiva
        StopPersonalization();

        // Anima a chama / feedback (opcional - aqui apenas um log)
        Debug.Log("Chama acesa. Iniciando modo de voo e MIDI...");

        // Notifica GameManager para iniciar o resto
        if (_gameManager != null)
        {
            _gameManager.StartExperienceFromDesign(_currentDesign);
        }

        OnPersonalizationConfirmed?.Invoke();
    }
}
