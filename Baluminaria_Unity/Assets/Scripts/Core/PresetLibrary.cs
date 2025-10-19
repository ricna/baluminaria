using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Carrega e mant�m presets de Balumin�rias.
/// Os arquivos ficam em Resources/Presets/*.json.
/// </summary>
public static class PresetLibrary
{
    private static Dictionary<string, BaluminariaData> _cache = new Dictionary<string, BaluminariaData>();

    /// <summary>
    /// Lista todos os nomes dispon�veis dentro de Resources/Presets.
    /// </summary>
    public static List<string> GetAvailablePresetNames()
    {
        List<string> names = new List<string>();
        TextAsset[] assets = Resources.LoadAll<TextAsset>("Presets");
        foreach (TextAsset asset in assets)
        {
            names.Add(asset.name);
        }
        return names;
    }

    /// <summary>
    /// Retorna um preset carregado a partir do nome (sem extens�o).
    /// </summary>
    public static BaluminariaData LoadPreset(string presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return null;

        if (_cache.ContainsKey(presetName))
        {
            return _cache[presetName];
        }

        TextAsset jsonFile = Resources.Load<TextAsset>($"Presets/{presetName}");
        if (jsonFile == null)
        {
            Debug.LogWarning($"Preset '{presetName}' n�o encontrado em Resources/Presets/");
            return null;
        }

        try
        {
            BaluminariaData data = JsonUtility.FromJson<BaluminariaData>(jsonFile.text);
            _cache[presetName] = data;
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"Erro ao carregar preset '{presetName}': {e.Message}");
            return null;
        }
    }
}
