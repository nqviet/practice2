using System.Collections;
using Game.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Game.Runtime.GameFlow
{
    /// <summary>
    /// Persistent (DontDestroyOnLoad) scene switcher: 00_Boot → 01_MainMenu ⇄ 02_Gameplay.
    /// One load at a time; always leaves timeScale at 1 so a paused scene cannot freeze the next.
    /// PLACEHOLDER: no transition curtain yet — a fade overlay can hook OnLoadStarted / OnLoadCompleted.
    /// </summary>
    public class SceneLoader : MonoBehaviour, ISceneLoader
    {
        private Coroutine m_LoadRoutine;

        public bool IsLoading => m_LoadRoutine != null;
        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        public UnityAction<string> OnLoadStarted;
        public UnityAction<string> OnLoadCompleted;

        public void Load(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneLoader] Empty scene name.");
                return;
            }

            if (IsLoading)
            {
                Debug.LogWarning($"[SceneLoader] Ignoring load of '{sceneName}': a load is already in progress.");
                return;
            }

            m_LoadRoutine = StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            Time.timeScale = 1f;
            OnLoadStarted?.Invoke(sceneName);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            if (op == null)
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in the build settings.");
                m_LoadRoutine = null;
                yield break;
            }

            while (!op.isDone)
            {
                yield return null;
            }

            m_LoadRoutine = null;
            OnLoadCompleted?.Invoke(sceneName);
        }
    }
}
