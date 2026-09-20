using UnityEngine;

namespace Game.Utils
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T s_Instance;
        private static readonly object s_Lock = new object();
        private static bool s_IsApplicationQuitting;

        public static T Instance
        {
            get
            {
                if (s_IsApplicationQuitting)
                {
                    return null;
                }

                lock (s_Lock)
                {
                    if (s_Instance == null)
                    {
                        s_Instance = FindFirstObjectByType<T>();
                    }

                    return s_Instance;
                }
            }
        }

        public static bool HasInstance => s_Instance != null;

        protected virtual void Awake()
        {
            if (s_Instance == null)
            {
                s_Instance = this as T;
            }
            else if (s_Instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            s_IsApplicationQuitting = true;
        }
    }
}
