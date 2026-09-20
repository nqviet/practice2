using System;

namespace Game.Core
{
    public interface IAnalyticsService
    {
        void LogEvent(string eventName);
        void LogLevelStart(int levelId);
        void LogLevelComplete(int levelId, int shotsTaken);
    }

    public interface IAdService
    {
        float BannerUnits { get; }
        event Action<float> OnBannerReserveChanged;
        void ShowBanner();
        void HideBanner();
        void ShowInterstitial(Action onClosed);
        bool IsInterstitialReady { get; }
    }

    public interface IIAPService
    {
        void Initialize(Action onSuccess, Action<string> onFailure);
        void PurchaseRemoveAds(Action onSuccess, Action<string> onFailure);
        bool HasRemovedAds { get; }
    }

    public interface ISaveService
    {
        PlayerData Data { get; }
        void Save();
        void Load();
        void ResetData();

        /// <summary>0-based index into the active LevelPack of the furthest level reached.</summary>
        int HighestUnlockedLevel { get; set; }

        /// <summary>0-based index into the active LevelPack of the level Play resumes.</summary>
        int CurrentLevelIndex { get; set; }

        bool AdsRemoved { get; set; }
    }

    public interface ISceneLoader
    {
        bool IsLoading { get; }
        string ActiveSceneName { get; }
        void Load(string sceneName);
    }

    public interface IAudioService
    {
        void Play(SfxId sfx, float pitch = 1.0f, float volume = 1.0f);
        void PlayChainCombo(int depth);
        void DuckMusic(float duration, float duckVolume = 0.2f);
        void SetMusicVolume(float volume);
        void SetSfxVolume(float volume);
    }

    public interface IVFXService
    {
        void PlayBreak(UnityEngine.Vector2 worldPos, GameColor color, int depth = 0);
        void PlayImpact(UnityEngine.Vector2 worldPos, float intensity = 1.0f);
        void PlayMuzzleFlash(UnityEngine.Vector2 worldPos, UnityEngine.Vector2 direction);
        void PlayRadialFlash(UnityEngine.Vector2 worldPos, UnityEngine.Color color, float startScale = 0.3f, float endScale = 1.4f, float duration = 0.25f);
        void RetainSettleToken();
        void ReleaseSettleToken();
        bool IsSettling { get; }
    }

    public interface ICameraShaker
    {
        void Shake(float amplitude = 1.0f);
    }

    public interface IScreenFeedback
    {
        void Flash(UnityEngine.Color color, float duration = 0.2f);
        void PulseVignette(UnityEngine.Color color, float duration = 0.5f);
    }
}
