using System;
using System.IO;
using UnityEngine;

namespace Unrez.BackyardShowdown
{
    /// <summary>
    /// Classe estática e centralizada para gerenciar o salvamento, carregamento e aplicação 
    /// das configurações do jogo usando JSON.
    /// </summary>
    public static class GameSettingsManager
    {
        private static readonly string _settingsFilePath = Path.Combine(Application.persistentDataPath, "GameSettings.json");

        private static GameSettingsData _settings;

        /// <summary>
        /// Propriedade estática para acesso seguro e preguiçoso (Lazy Loading).
        /// </summary>
        public static GameSettingsData Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = LoadSettings();
                }
                return _settings;
            }
        }

        // --- Métodos de Serialização e Desserialização (JSON) ---

        private static GameSettingsData LoadSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    GameSettingsData loadedSettings = JsonUtility.FromJson<GameSettingsData>(json);
                    Debug.Log("Game settings loaded successfully.");
                    return loadedSettings;
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error loading settings file: {e.Message}. Creating default settings.");
                    return CreateDefaultSettings();
                }
            }
            else
            {
                return CreateDefaultSettings();
            }
        }

        private static GameSettingsData CreateDefaultSettings()
        {
            GameSettingsData defaultSettings = new GameSettingsData();
            _settings = defaultSettings;
            SaveSettings(); // Salva o novo arquivo padrão imediatamente
            Debug.Log("Settings file not found. Created and saved default settings.");
            return defaultSettings;
        }

        /// <summary>
        /// Salva as configurações atuais em memória para o disco.
        /// </summary>
        public static void SaveSettings()
        {
            if (_settings == null)
            {
                LoadSettings();
            }

            try
            {
                string json = JsonUtility.ToJson(_settings, true);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error saving settings file: {e.Message}");
            }
        }

        /// <summary>
        /// Aplica as configurações do objeto aos sistemas do jogo.
        /// Deve ser chamado no início do jogo e após carregar as configurações.
        /// </summary>
        public static void ApplySettings()
        {
            GameSettingsData currentSettings = Settings;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.SetMasterVolume(currentSettings.masterVolume); // NOVO
                AudioManager.Instance.SetMusicVolume(currentSettings.musicVolume);
                AudioManager.Instance.SetSFXVolume(currentSettings.sfxVolume);
            }
            QualitySettings.vSyncCount = currentSettings.vsync ? 1 : 0;
            Screen.fullScreen = currentSettings.fullscreen;
            Debug.Log("All game settings applied to the game systems.");
        }

    }
}