using System;
using UnityEngine;

namespace Unrez.BackyardShowdown
{
    [Serializable]
    public class GameSettingsData
    {
        // O JsonUtility serializa enums como strings por padrão.
        public SystemLanguage language = SystemLanguage.English;
        public float masterVolume = 1f; 
        public float sfxVolume = 1f;
        public float musicVolume = 1f;
        public bool vsync = true;
        public bool fullscreen = true;

        /// <summary>
        /// Construtor para definir valores iniciais baseados no sistema.
        /// </summary>
        public GameSettingsData()
        {
            // Define a linguagem padrão para Português se for a linguagem do sistema, senão English.
            SystemLanguage systemLang = Application.systemLanguage;
            if (systemLang == SystemLanguage.Portuguese)
            {
                language = SystemLanguage.Portuguese;
            }
            else
            {
                language = SystemLanguage.English;
            }
        }
    }
}