using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Unrez.Essential;

namespace Unrez.BackyardShowdown
{
    public class AudioManager : Singleton<AudioManager>
    {
        // --- CONSTANTES ---
        private const string MASTER_VOLUME_PARAM = "MasterVolume"; // NOVO
        private const string MUSIC_VOLUME_PARAM = "MusicVolume";
        private const string SFX_VOLUME_PARAM = "SFXVolume";
        private const float MIN_VOLUME_DB = -80f;
        private const float MAX_VOLUME_DB = 0f;

        // --- CONFIGURAÇÃO DO UNITY (Inspector) ---

        [Header("Audio Mixer Groups")]
        [SerializeField]
        private AudioMixer _audioMixer;
        [SerializeField]
        private AudioMixerGroup _musicGroup;
        [SerializeField]
        private AudioMixerGroup _sfxGroup;

        [Header("SFX Pool Settings")]
        [SerializeField]
        private int _minPoolSize = 8;
        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private Transform _sfxPoolParent;
        private Dictionary<AudioSource, Coroutine> _activeLoopFades = new Dictionary<AudioSource, Coroutine>();

        private AudioSource _musicSource;
        private Coroutine _musicFadeRoutine;

        [Header("SFX Button")]
        [SerializeField]
        private AudioClip[] _sfxButtonClick;
        [SerializeField]
        private AudioClip[] _sfxButtonSelect;
        [SerializeField]
        private AudioClip[] _sfxButtonSoftClick;
        [SerializeField]
        private AudioClip[] _sfxButtonSoftSelect;
        [SerializeField]
        private AudioClip[] _sfxButtonHeavyClick;
        [SerializeField]
        private AudioClip[] _sfxButtonHeavySelect;


        protected override void Awake()
        {
            base.Awake();

            if (this == null) return;

            SetupAudioSources();
            InitializePool();
        }

        private void SetupAudioSources()
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            if (_musicGroup != null)
            {
                _musicSource.outputAudioMixerGroup = _musicGroup;
            }
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;

            _sfxPoolParent = new GameObject("SFX_Pool_Sources").transform;
            _sfxPoolParent.SetParent(transform);
        }

        private void InitializePool()
        {
            for (int i = 0; i < _minPoolSize; i++)
            {
                CreateNewSfxAudioSource();
            }
        }

        private AudioSource CreateNewSfxAudioSource()
        {
            GameObject newSfxGO = new GameObject($"SFX_Source_{_sfxPool.Count:00}");
            newSfxGO.transform.SetParent(_sfxPoolParent);
            AudioSource newSource = newSfxGO.AddComponent<AudioSource>();

            if (_sfxGroup != null)
            {
                newSource.outputAudioMixerGroup = _sfxGroup;
            }
            newSource.playOnAwake = false;
            newSource.spatialBlend = 0f;

            _sfxPool.Add(newSource);
            return newSource;
        }

        public void SetGroupVolume(AudioMixerGroup mixerGroup, float linearVolume, string volumeParameterName)
        {
            if (mixerGroup == null)
            {
                Debug.LogWarning($"AudioMixerGroup não atribuído para o parâmetro: {volumeParameterName}.");
                return;
            }

            mixerGroup.audioMixer.SetFloat(volumeParameterName, GetDbVolume(linearVolume));
        }
        public void SetMasterVolume(float volume)
        {
            _audioMixer.SetFloat(MASTER_VOLUME_PARAM, GetDbVolume(volume));
        }

        /// <summary>
        /// O método GetDbVolume foi criado para encapsular a conversão da escala de volume linear (0 a 1) para a 
        /// escala logarítmica (decibéis - dB), que é o formato exigido pelo AudioMixer da Unity.
        /// 1. Por que é Necessário? (A Razão do Decibel)
        /// O áudio percebido por humanos é logarítmico.
        /// Um volume linear(como o valor 0.5 de um slider) não soa como "metade do volume" para o ouvido humano; soa muito mais alto.
        /// Para que um slider (que usa a escala linear de 0 a 1) pareça ter uma progressão de volume suave e natural para o jogador, 
        /// precisamos converter esse valor linear para a escala de decibéis(dB). O decibel é uma unidade logarítmica que mapeia melhor a sensibilidade do ouvido.
        /// </summary>
        /// <param name="linearVolume"></param>
        /// <returns></returns>
        private float GetDbVolume(float linearVolume)
        {
            float clampedVolume = Mathf.Clamp(linearVolume, 0.0001f, 1f);
            float dbVolume = Mathf.Log10(clampedVolume) * 20;
            dbVolume = Mathf.Clamp(dbVolume, MIN_VOLUME_DB, MAX_VOLUME_DB);
            return dbVolume;
        }

        public void SetMusicVolume(float volume)
        {
            SetGroupVolume(_musicGroup, volume, MUSIC_VOLUME_PARAM);
        }

        public void SetSFXVolume(float volume)
        {
            SetGroupVolume(_sfxGroup, volume, SFX_VOLUME_PARAM);
        }

        // --- REPRODUÇÃO DE MÚSICA (FADE) ---

        public void PlayMusic(AudioClip musicClip, float volume = 1f, float fadeDuration = 0f)
        {
            if (musicClip == null) return;

            if (_musicSource.isPlaying && _musicSource.clip == musicClip) return;

            _musicSource.clip = musicClip;
            _musicSource.loop = true;

            if (fadeDuration > 0f)
            {
                if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
                _musicFadeRoutine = StartCoroutine(MusicFadeIn(volume, fadeDuration));
            }
            else
            {
                _musicSource.volume = volume;
                _musicSource.Play();
                // Aplica o volume do usuário (Mixer)
                SetMusicVolume(GameSettingsManager.Settings.musicVolume);
            }
        }

        public void StopMusic(float fadeDuration = 0f)
        {
            if (!_musicSource.isPlaying) return;

            if (fadeDuration > 0f)
            {
                if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
                _musicFadeRoutine = StartCoroutine(MusicFadeOut(fadeDuration));
            }
            else
            {
                _musicSource.Stop();
            }
        }

        private IEnumerator MusicFadeIn(float targetVolume, float duration)
        {
            _musicSource.volume = 0f;
            _musicSource.Play();
            float startVolume = 0f;
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
                yield return null;
            }

            _musicSource.volume = targetVolume;
        }

        private IEnumerator MusicFadeOut(float duration)
        {
            float startVolume = _musicSource.volume;
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, time / duration);
                yield return null;
            }

            _musicSource.Stop();
            _musicSource.volume = startVolume;
        }

        // --- REPRODUÇÃO DE SFX (Pool) ---

        private AudioSource GetAvailableSfxAudioSource()
        {
            foreach (AudioSource source in _sfxPool)
            {
                if (!source.isPlaying)
                {
                    source.Stop();
                    source.clip = null;
                    source.volume = 1f;
                    source.pitch = 1f;
                    source.loop = false; // Garante que não está em loop
                    return source;
                }
            }

            // Se todas estiverem em uso, cria e retorna uma nova
            Debug.LogWarning("Pool de SFX esgotado. Criando nova AudioSource.");
            return CreateNewSfxAudioSource();
        }

        public void PlaySFX(AudioClip sfxClip, float volume = 1f, float pitch = 1f)
        {
            if (sfxClip == null) return;

            AudioSource source = GetAvailableSfxAudioSource();
            if (source != null)
            {
                source.clip = sfxClip;
                source.volume = volume;
                source.pitch = pitch;
                source.Play();
            }
        }

        // --- SFX EM LOOP (Efeitos Contínuos com FADE) ---

        public AudioSource PlaySFXLoop(AudioClip sfxClip, float volume = 1f, float fadeDuration = 0.5f)
        {
            if (sfxClip == null) return null;

            AudioSource loopSource = GetAvailableSfxAudioSource();

            if (loopSource != null)
            {
                if (_activeLoopFades.ContainsKey(loopSource) && _activeLoopFades[loopSource] != null)
                {
                    StopCoroutine(_activeLoopFades[loopSource]);
                    _activeLoopFades.Remove(loopSource);
                }

                loopSource.clip = sfxClip;
                loopSource.loop = true;

                Coroutine routine = StartCoroutine(SFXFade(loopSource, volume, fadeDuration, true));
                _activeLoopFades.Add(loopSource, routine);
            }

            return loopSource;
        }

        public void StopSFXLoop(AudioSource audioSource, float fadeDuration = 0.5f)
        {
            if (audioSource == null || !audioSource.isPlaying) return;

            if (_activeLoopFades.ContainsKey(audioSource) && _activeLoopFades[audioSource] != null)
            {
                StopCoroutine(_activeLoopFades[audioSource]);
                _activeLoopFades.Remove(audioSource);
            }

            Coroutine routine = StartCoroutine(SFXFade(audioSource, 0f, fadeDuration, false));
            _activeLoopFades.Add(audioSource, routine);
        }

        private IEnumerator SFXFade(AudioSource audioSource, float targetVolume, float duration, bool fadeIn)
        {
            float startVolume = audioSource.volume;

            if (fadeIn)
            {
                audioSource.volume = 0f;
                audioSource.Play();
            }

            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                audioSource.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
                yield return null;
            }

            audioSource.volume = targetVolume;

            if (!fadeIn && targetVolume == 0f)
            {
                audioSource.Stop();
                audioSource.loop = false;
                if (_activeLoopFades.ContainsKey(audioSource))
                {
                    _activeLoopFades.Remove(audioSource);
                }
            }
        }

        // --- SFX de UI (Botões) ---

        public void PlaySFXButtonSelect(ButtonSFXType effect)
        {
            switch (effect)
            {

                case ButtonSFXType.Default:
                    PlayRandomSFXFromArray(_sfxButtonSelect);
                    break;
                case ButtonSFXType.Soft:
                    PlayRandomSFXFromArray(_sfxButtonSoftSelect);
                    break;
                case ButtonSFXType.Heavy:
                    PlayRandomSFXFromArray(_sfxButtonHeavySelect);
                    break;
                case ButtonSFXType.None:
                default:
                    break;
            }
        }

        public void PlaySFXButtonClick(ButtonSFXType effect)
        {
            switch (effect)
            {
                case ButtonSFXType.Default:
                    PlayRandomSFXFromArray(_sfxButtonClick);
                    break;
                case ButtonSFXType.Soft:
                    PlayRandomSFXFromArray(_sfxButtonSoftClick);
                    break;
                case ButtonSFXType.Heavy:
                    PlayRandomSFXFromArray(_sfxButtonHeavyClick);
                    break;
                case ButtonSFXType.None:
                default:
                    break;
            }
        }

        private void PlayRandomSFXFromArray(AudioClip[] targetArray)
        {
            if (targetArray == null || targetArray.Length == 0) return;

            AudioClip clipToPlay = targetArray[Random.Range(0, targetArray.Length)];
            PlaySFX(clipToPlay);
        }
    }
}