using UnityEngine;

/// <summary>
/// Controla o modo escuro / preview da lâmpada interna.
/// </summary>
public class LightingPreviewManager : MonoBehaviour
{
    [SerializeField] private Light _baluminariaInnerLight; // point light dentro da baluminária
    [SerializeField] private Light[] _sceneLights; // lumières globais da cena
    [SerializeField] private float _darkInnerIntensity = 2.5f;
    [SerializeField] private float _defaultInnerIntensity = 0f;
    [SerializeField] private float _defaultAmbientIntensity = 0.8f;
    [SerializeField] private float _darkAmbientIntensity = 0.02f;

    private bool _isDark = false;

    private void Start()
    {
        ApplyLightingMode();
    }

    public void SetDarkMode(bool enable)
    {
        _isDark = enable;
        ApplyLightingMode();
    }

    public void ToggleDarkMode()
    {
        _isDark = !_isDark;
        ApplyLightingMode();
    }

    public bool IsDarkMode()
    {
        return _isDark;
    }

    private void ApplyLightingMode()
    {
        if (_isDark)
        {
            // apagar luzes da cena
            for (int i = 0; i < _sceneLights.Length; i++)
            {
                Light sceneLight = _sceneLights[i];
                if (sceneLight != null) sceneLight.enabled = false;
            }
            if (_baluminariaInnerLight != null)
            {
                _baluminariaInnerLight.enabled = true;
                _baluminariaInnerLight.intensity = _darkInnerIntensity;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black * _darkAmbientIntensity;
        }
        else
        {
            for (int i = 0; i < _sceneLights.Length; i++)
            {
                Light sceneLight = _sceneLights[i];
                if (sceneLight != null) sceneLight.enabled = true;
            }
            if (_baluminariaInnerLight != null)
            {
                _baluminariaInnerLight.enabled = false;
                _baluminariaInnerLight.intensity = _defaultInnerIntensity;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white * _defaultAmbientIntensity;
        }
    }
}
