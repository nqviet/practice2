using System.Collections;
using System.Collections.Generic;
using Game.Core;
using Game.Utils;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Runtime.Services
{
    public class AudioService : Singleton<AudioService>, IAudioService
    {
        [Header("Audio Mixer & Groups")]
        [SerializeField] private AudioMixer m_Mixer;
        [SerializeField] private AudioMixerGroup m_MasterGroup;
        [SerializeField] private AudioMixerGroup m_MusicGroup;
        [SerializeField] private AudioMixerGroup m_SfxGroup;
        [SerializeField] private AudioMixerGroup m_UiGroup;

        [Header("Audio Clips (Optional - Fallback Procedural Audio Used If Null)")]
        [SerializeField] private AudioClip m_ClipPlink;
        [SerializeField] private AudioClip m_ClipFire;
        [SerializeField] private AudioClip m_ClipBounce;
        [SerializeField] private AudioClip m_ClipDetonation;
        [SerializeField] private AudioClip m_ClipChainCombo;
        [SerializeField] private AudioClip m_ClipBallReturn;
        [SerializeField] private AudioClip m_ClipRestart;
        [SerializeField] private AudioClip m_ClipWin;
        [SerializeField] private AudioClip m_ClipUIClick;

        [Header("Music Source")]
        [SerializeField] private AudioSource m_MusicSource;

        private const int VoicePoolSize = 12;
        private const string MixerMusicParam = "MusicVolume";
        private const string MixerSfxParam = "SfxVolume";
        private const float MinDecibels = -80f;

        private readonly List<AudioSource> m_VoicePool = new List<AudioSource>();
        private readonly Dictionary<SfxId, AudioClip> m_ClipMap = new Dictionary<SfxId, AudioClip>();
        private Coroutine m_DuckCoroutine;
        private float m_MusicOriginalVolume = 0.7f;
        private float m_MusicVolumeScale = 1.0f;
        private float m_SfxVolumeScale = 1.0f;

        public float MusicVolume => m_MusicVolumeScale;
        public float SfxVolume => m_SfxVolumeScale;

        protected override void Awake()
        {
            base.Awake();
            ServiceLocator.Register<IAudioService>(this);
            InitializeVoicePool();
            InitializeClips();
            ApplySavedSettings();
        }

        /// <summary>Pulls the persisted music/SFX levels (settings popup) from the save service.</summary>
        public void ApplySavedSettings()
        {
            var save = ServiceLocator.Get<ISaveService>();
            if (save == null || save.Data == null) return;

            SetMusicVolume(save.Data.EffectiveMusicVolume);
            SetSfxVolume(save.Data.EffectiveSfxVolume);
        }

        public void SetMusicVolume(float volume)
        {
            m_MusicVolumeScale = Mathf.Clamp01(volume);
            if (m_MusicSource != null && m_DuckCoroutine == null)
            {
                m_MusicSource.volume = m_MusicOriginalVolume * m_MusicVolumeScale;
            }
            SetMixerVolume(MixerMusicParam, m_MusicVolumeScale);
        }

        public void SetSfxVolume(float volume)
        {
            m_SfxVolumeScale = Mathf.Clamp01(volume);
            SetMixerVolume(MixerSfxParam, m_SfxVolumeScale);
        }

        private void SetMixerVolume(string exposedParam, float linear)
        {
            if (m_Mixer == null) return;

            // Exposed parameters are optional; SetFloat simply returns false when one is missing.
            float db = linear > 0.0001f ? Mathf.Log10(linear) * 20f : MinDecibels;
            m_Mixer.SetFloat(exposedParam, db);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ServiceLocator.Unregister<IAudioService>();
        }

        private void InitializeVoicePool()
        {
            for (int i = 0; i < VoicePoolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D sound
                if (m_SfxGroup != null)
                {
                    source.outputAudioMixerGroup = m_SfxGroup;
                }
                m_VoicePool.Add(source);
            }

            if (m_MusicSource == null)
            {
                m_MusicSource = gameObject.AddComponent<AudioSource>();
                m_MusicSource.playOnAwake = false;
                m_MusicSource.loop = true;
                m_MusicSource.spatialBlend = 0f;
                if (m_MusicGroup != null)
                {
                    m_MusicSource.outputAudioMixerGroup = m_MusicGroup;
                }
            }
            m_MusicOriginalVolume = m_MusicSource.volume;
        }

        private void InitializeClips()
        {
            // Bind provided clips or generate procedural fallbacks
            m_ClipMap[SfxId.Plink] = m_ClipPlink != null ? m_ClipPlink : GenerateProceduralClip(SfxId.Plink);
            m_ClipMap[SfxId.Fire] = m_ClipFire != null ? m_ClipFire : GenerateProceduralClip(SfxId.Fire);
            m_ClipMap[SfxId.Bounce] = m_ClipBounce != null ? m_ClipBounce : GenerateProceduralClip(SfxId.Bounce);
            m_ClipMap[SfxId.Detonation] = m_ClipDetonation != null ? m_ClipDetonation : GenerateProceduralClip(SfxId.Detonation);
            m_ClipMap[SfxId.ChainCombo] = m_ClipChainCombo != null ? m_ClipChainCombo : GenerateProceduralClip(SfxId.ChainCombo);
            m_ClipMap[SfxId.BallReturn] = m_ClipBallReturn != null ? m_ClipBallReturn : GenerateProceduralClip(SfxId.BallReturn);
            m_ClipMap[SfxId.Restart] = m_ClipRestart != null ? m_ClipRestart : GenerateProceduralClip(SfxId.Restart);
            m_ClipMap[SfxId.Win] = m_ClipWin != null ? m_ClipWin : GenerateProceduralClip(SfxId.Win);
            m_ClipMap[SfxId.UIClick] = m_ClipUIClick != null ? m_ClipUIClick : GenerateProceduralClip(SfxId.UIClick);
        }

        public void Play(SfxId sfx, float pitch = 1.0f, float volume = 1.0f)
        {
            if (sfx == SfxId.None || m_SfxVolumeScale <= 0f) return;

            if (!m_ClipMap.TryGetValue(sfx, out AudioClip clip) || clip == null)
            {
                return;
            }

            AudioSource voice = GetAvailableVoice();
            if (voice != null)
            {
                voice.pitch = Mathf.Clamp(pitch, 0.5f, 3.0f);
                voice.volume = Mathf.Clamp01(volume) * m_SfxVolumeScale;
                voice.PlayOneShot(clip);
            }
        }

        public static float CalculatePitch(int depth)
        {
            // Ascending musical pitch ladder: whole-tone steps (2 semitones per depth link) (§4.1, §6.4, gameplay.md §10)
            return Mathf.Pow(2.0f, (depth * 2f) / 12f);
        }

        public void PlayChainCombo(int depth)
        {
            float pitch = CalculatePitch(depth);
            Play(SfxId.ChainCombo, pitch);
        }

        public void DuckMusic(float duration, float duckVolume = 0.2f)
        {
            if (m_MusicSource == null) return;

            if (m_DuckCoroutine != null)
            {
                StopCoroutine(m_DuckCoroutine);
            }
            m_DuckCoroutine = StartCoroutine(DuckRoutine(duration, duckVolume));
        }

        private IEnumerator DuckRoutine(float duration, float duckVolume)
        {
            float startVol = m_MusicOriginalVolume * m_MusicVolumeScale;
            m_MusicSource.volume = Mathf.Min(duckVolume, startVol);

            yield return new WaitForSecondsRealtime(duration);

            float t = 0f;
            const float restoreDuration = 0.4f;
            while (t < restoreDuration)
            {
                t += Time.unscaledDeltaTime;
                startVol = m_MusicOriginalVolume * m_MusicVolumeScale;
                m_MusicSource.volume = Mathf.Lerp(Mathf.Min(duckVolume, startVol), startVol, t / restoreDuration);
                yield return null;
            }

            m_MusicSource.volume = m_MusicOriginalVolume * m_MusicVolumeScale;
            m_DuckCoroutine = null;
        }

        private AudioSource GetAvailableVoice()
        {
            for (int i = 0; i < m_VoicePool.Count; i++)
            {
                if (!m_VoicePool[i].isPlaying)
                {
                    return m_VoicePool[i];
                }
            }

            // If all busy, steal voice 0
            if (m_VoicePool.Count > 0)
            {
                return m_VoicePool[0];
            }

            return null;
        }

        private static AudioClip GenerateProceduralClip(SfxId sfx)
        {
            const int sampleRate = 44100;
            float duration;
            switch (sfx)
            {
                case SfxId.Plink: duration = 0.12f; break;
                case SfxId.Fire: duration = 0.18f; break;
                case SfxId.Bounce: duration = 0.05f; break;
                case SfxId.Detonation: duration = 0.35f; break;
                case SfxId.ChainCombo: duration = 0.25f; break;
                case SfxId.BallReturn: duration = 0.25f; break;
                case SfxId.Restart: duration = 0.15f; break;
                case SfxId.Win: duration = 0.60f; break;
                case SfxId.UIClick: duration = 0.06f; break;
                default: duration = 0.1f; break;
            }

            int samplesCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[samplesCount];

            for (int i = 0; i < samplesCount; i++)
            {
                float t = (float)i / sampleRate;
                float normT = (float)i / samplesCount;
                float env = 1.0f - normT;

                switch (sfx)
                {
                    case SfxId.Plink:
                        // High bell plink (880 Hz)
                        samples[i] = Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-12f * normT) * 0.6f;
                        break;

                    case SfxId.Fire:
                        // Low bass thoomp dropping from 140Hz to 40Hz
                        float freq = Mathf.Lerp(140f, 40f, normT);
                        float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                        float noise = (Random.value * 2f - 1f) * 0.15f * Mathf.Exp(-8f * normT);
                        samples[i] = (sine * 0.7f + noise) * Mathf.Exp(-6f * normT);
                        break;

                    case SfxId.Bounce:
                        // Crisp tick / pop
                        samples[i] = Mathf.Sin(2f * Mathf.PI * 1800f * t) * Mathf.Exp(-40f * normT) * 0.5f;
                        break;

                    case SfxId.Detonation:
                        // Heavy crack + bass explosion
                        float crack = (Random.value * 2f - 1f) * Mathf.Exp(-15f * normT);
                        float boom = Mathf.Sin(2f * Mathf.PI * 65f * t) * Mathf.Exp(-4f * normT);
                        samples[i] = (crack * 0.5f + boom * 0.6f) * 0.8f;
                        break;

                    case SfxId.ChainCombo:
                        // Resonant chime (523.25 Hz C5 + octave harmonic)
                        float c1 = Mathf.Sin(2f * Mathf.PI * 523.25f * t);
                        float c2 = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.5f;
                        samples[i] = (c1 + c2) * Mathf.Exp(-7f * normT) * 0.6f;
                        break;

                    case SfxId.BallReturn:
                        // Soft whoosh
                        float whooshNoise = (Random.value * 2f - 1f);
                        float sweep = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(300f, 150f, normT) * t);
                        samples[i] = (whooshNoise * 0.25f + sweep * 0.35f) * Mathf.Sin(Mathf.PI * normT);
                        break;

                    case SfxId.Restart:
                        // Rattle and click
                        float click1 = (t < 0.04f) ? Mathf.Sin(2f * Mathf.PI * 1200f * t) : 0f;
                        float click2 = (t > 0.08f) ? Mathf.Sin(2f * Mathf.PI * 1600f * (t - 0.08f)) * Mathf.Exp(-20f * (t - 0.08f)) : 0f;
                        samples[i] = (click1 + click2) * 0.5f;
                        break;

                    case SfxId.Win:
                        // Fanfare chord progression
                        float winFreq = 523.25f; // C5
                        if (normT > 0.2f) winFreq = 659.25f; // E5
                        if (normT > 0.4f) winFreq = 783.99f; // G5
                        if (normT > 0.6f) winFreq = 1046.50f; // C6
                        samples[i] = Mathf.Sin(2f * Mathf.PI * winFreq * t) * Mathf.Exp(-2f * normT) * 0.7f;
                        break;

                    case SfxId.UIClick:
                        // Short soft tick for buttons
                        samples[i] = Mathf.Sin(2f * Mathf.PI * 1320f * t) * Mathf.Exp(-30f * normT) * 0.35f;
                        break;

                    default:
                        samples[i] = 0f;
                        break;
                }
            }

            var clip = AudioClip.Create($"Sfx_{sfx}_Procedural", samplesCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
