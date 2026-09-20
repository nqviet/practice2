using Game.Core;
using Game.Runtime.GameFlow;
using Game.Runtime.Services;
using UnityEngine;

namespace Game.Runtime.Bootstrap
{
    /// <summary>
    /// Owner of the app-lifetime services (save, scene loading) on a DontDestroyOnLoad object.
    /// Created before the first scene loads, so 02_Gameplay / 01_MainMenu also run correctly
    /// when entered directly in the Editor without passing through 00_Boot.
    /// Ads / IAP / analytics register here later behind their Core interfaces (out of scope for M5).
    /// </summary>
    public class AppServices : MonoBehaviour
    {
        private const string HostName = "[SERVICES]";

        private static AppServices s_Instance;

        private SaveService m_SaveService;
        private SceneLoader m_SceneLoader;

        public static AppServices Instance => s_Instance;
        public ISaveService Save => m_SaveService;
        public ISceneLoader SceneLoader => m_SceneLoader;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            // Keeps "Enter Play Mode without domain reload" safe
            s_Instance = null;
            ServiceLocator.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            EnsureCreated();
        }

        /// <summary>Idempotent: creates and registers the service host once per app run.</summary>
        public static AppServices EnsureCreated()
        {
            if (s_Instance != null) return s_Instance;

            var host = new GameObject(HostName);
            DontDestroyOnLoad(host);
            s_Instance = host.AddComponent<AppServices>();
            s_Instance.Initialize();
            return s_Instance;
        }

        private void Initialize()
        {
            m_SaveService = new SaveService(SaveService.DefaultFilePath);
            m_SaveService.Load();
            ServiceLocator.Register<ISaveService>(m_SaveService);

            m_SceneLoader = gameObject.AddComponent<SceneLoader>();
            ServiceLocator.Register<ISceneLoader>(m_SceneLoader);
        }

        private void OnApplicationPause(bool paused)
        {
            // Mobile: the OS may kill a backgrounded app without OnApplicationQuit
            if (paused)
            {
                m_SaveService?.Save();
            }
        }

        private void OnApplicationQuit()
        {
            m_SaveService?.Save();
        }

        private void OnDestroy()
        {
            if (s_Instance != this) return;

            ServiceLocator.Unregister<ISaveService>();
            ServiceLocator.Unregister<ISceneLoader>();
            s_Instance = null;
        }
    }
}
