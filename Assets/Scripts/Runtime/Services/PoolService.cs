using System.Collections.Generic;
using Game.Core;
using Game.Utils;
using UnityEngine;

namespace Game.Runtime.Services
{
    public class PoolService : Singleton<PoolService>
    {
        [System.Serializable]
        public struct PoolMapping
        {
            public PoolId Id;
            public GameObject Prefab;
            public int InitialCapacity;
        }

        [SerializeField] private List<PoolMapping> m_ConfiguredPools = new List<PoolMapping>();
        [SerializeField] private Transform m_DefaultPoolRoot;

        private readonly Dictionary<GameObject, Queue<GameObject>> m_PrefabPools = new Dictionary<GameObject, Queue<GameObject>>();
        private readonly Dictionary<PoolId, GameObject> m_IdToPrefab = new Dictionary<PoolId, GameObject>();
        private readonly Dictionary<GameObject, List<GameObject>> m_ActiveInstances = new Dictionary<GameObject, List<GameObject>>();
        private int m_RuntimeInstantiateCount;

        public int RuntimeInstantiateCount => m_RuntimeInstantiateCount;
        public Transform DefaultPoolRoot => m_DefaultPoolRoot;

        protected override void Awake()
        {
            base.Awake();

            if (m_DefaultPoolRoot == null)
            {
                var poolRootGo = GameObject.Find("[VFX_POOL]");
                if (poolRootGo != null)
                {
                    m_DefaultPoolRoot = poolRootGo.transform;
                }
            }

            for (int i = 0; i < m_ConfiguredPools.Count; i++)
            {
                var mapping = m_ConfiguredPools[i];
                if (mapping.Prefab != null)
                {
                    RegisterPrefab(mapping.Id, mapping.Prefab);
                    if (mapping.InitialCapacity > 0)
                    {
                        Prewarm(mapping.Prefab, mapping.InitialCapacity, m_DefaultPoolRoot);
                    }
                }
            }
        }

        public void RegisterPrefab(PoolId id, GameObject prefab)
        {
            if (prefab == null) return;
            m_IdToPrefab[id] = prefab;
            if (!m_PrefabPools.ContainsKey(prefab))
            {
                m_PrefabPools[prefab] = new Queue<GameObject>();
                m_ActiveInstances[prefab] = new List<GameObject>();
            }
        }

        public void Prewarm(GameObject prefab, int count, Transform parent = null)
        {
            if (prefab == null || count <= 0) return;

            Transform targetParent = parent != null ? parent : (m_DefaultPoolRoot != null ? m_DefaultPoolRoot : transform);

            if (!m_PrefabPools.ContainsKey(prefab))
            {
                m_PrefabPools[prefab] = new Queue<GameObject>();
                m_ActiveInstances[prefab] = new List<GameObject>();
            }

            Queue<GameObject> queue = m_PrefabPools[prefab];

            for (int i = 0; i < count; i++)
            {
                GameObject instance = Instantiate(prefab, targetParent);
                instance.SetActive(false);

                var pooled = instance.GetComponent<PooledInstance>();
                if (pooled == null)
                {
                    pooled = instance.AddComponent<PooledInstance>();
                }
                pooled.PrefabOrigin = prefab;
                pooled.OwnerPool = this;
                pooled.IsSpawned = false;

                queue.Enqueue(instance);
            }
        }

        public void Prewarm<T>(T prefab, int count, Transform parent = null) where T : Component
        {
            if (prefab != null)
            {
                Prewarm(prefab.gameObject, count, parent);
            }
        }

        public void Prewarm(PoolId id, GameObject prefab, int count, Transform parent = null)
        {
            RegisterPrefab(id, prefab);
            Prewarm(prefab, count, parent);
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null) return null;

            if (!m_PrefabPools.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                m_PrefabPools[prefab] = queue;
                m_ActiveInstances[prefab] = new List<GameObject>();
            }

            GameObject instance = null;
            while (queue.Count > 0)
            {
                GameObject candidate = queue.Dequeue();
                if (candidate != null)
                {
                    instance = candidate;
                    break;
                }
            }

            if (instance == null)
            {
                m_RuntimeInstantiateCount++;
                Transform targetParent = parent != null ? parent : (m_DefaultPoolRoot != null ? m_DefaultPoolRoot : transform);
                instance = Instantiate(prefab, position, rotation, targetParent);
            }
            else
            {
                instance.transform.SetPositionAndRotation(position, rotation);
                if (parent != null)
                {
                    instance.transform.SetParent(parent);
                }
            }

            var pooled = instance.GetComponent<PooledInstance>();
            if (pooled == null)
            {
                pooled = instance.AddComponent<PooledInstance>();
            }
            pooled.PrefabOrigin = prefab;
            pooled.OwnerPool = this;
            pooled.IsSpawned = true;

            m_ActiveInstances[prefab].Add(instance);
            instance.SetActive(true);

            return instance;
        }

        public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            if (prefab == null) return null;
            GameObject go = Spawn(prefab.gameObject, position, rotation, parent);
            return go != null ? go.GetComponent<T>() : null;
        }

        public GameObject Spawn(PoolId id, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (m_IdToPrefab.TryGetValue(id, out GameObject prefab))
            {
                return Spawn(prefab, position, rotation, parent);
            }

            Debug.LogWarning($"[PoolService] No prefab registered for PoolId '{id}'");
            return null;
        }

        public T Spawn<T>(PoolId id, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            GameObject go = Spawn(id, position, rotation, parent);
            return go != null ? go.GetComponent<T>() : null;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null) return;

            var pooled = instance.GetComponent<PooledInstance>();
            if (pooled != null && pooled.PrefabOrigin != null)
            {
                GameObject prefab = pooled.PrefabOrigin;
                if (!m_PrefabPools.TryGetValue(prefab, out Queue<GameObject> queue))
                {
                    queue = new Queue<GameObject>();
                    m_PrefabPools[prefab] = queue;
                    m_ActiveInstances[prefab] = new List<GameObject>();
                }

                if (m_ActiveInstances.TryGetValue(prefab, out List<GameObject> activeList))
                {
                    activeList.Remove(instance);
                }

                pooled.IsSpawned = false;
                instance.SetActive(false);

                if (m_DefaultPoolRoot != null && instance.transform.parent != m_DefaultPoolRoot)
                {
                    instance.transform.SetParent(m_DefaultPoolRoot);
                }

                queue.Enqueue(instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        public void Despawn(Component instance)
        {
            if (instance != null)
            {
                Despawn(instance.gameObject);
            }
        }

        public void ReleaseAll(GameObject prefab)
        {
            if (prefab == null) return;

            if (m_ActiveInstances.TryGetValue(prefab, out List<GameObject> activeList))
            {
                var listCopy = new List<GameObject>(activeList);
                for (int i = 0; i < listCopy.Count; i++)
                {
                    if (listCopy[i] != null)
                    {
                        Despawn(listCopy[i]);
                    }
                }
            }
        }

        public void ReleaseAll(PoolId id)
        {
            if (m_IdToPrefab.TryGetValue(id, out GameObject prefab))
            {
                ReleaseAll(prefab);
            }
        }

        public int GetActiveCount(GameObject prefab)
        {
            if (prefab != null && m_ActiveInstances.TryGetValue(prefab, out List<GameObject> activeList))
            {
                return activeList.Count;
            }
            return 0;
        }

        public int GetPooledCount(GameObject prefab)
        {
            if (prefab != null && m_PrefabPools.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                return queue.Count;
            }
            return 0;
        }

        public void ResetRuntimeInstantiateCount()
        {
            m_RuntimeInstantiateCount = 0;
        }
    }
}
