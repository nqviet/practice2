using Game.Core;
using UnityEngine;

namespace Game.Runtime.Services
{
    [DisallowMultipleComponent]
    public class PooledInstance : MonoBehaviour
    {
        [SerializeField] private GameObject m_PrefabOrigin;
        [SerializeField] private PoolId m_PoolId = PoolId.None;
        [SerializeField] private bool m_IsSpawned;

        private PoolService m_OwnerPool;

        public GameObject PrefabOrigin
        {
            get => m_PrefabOrigin;
            set => m_PrefabOrigin = value;
        }

        public PoolId PoolId
        {
            get => m_PoolId;
            set => m_PoolId = value;
        }

        public PoolService OwnerPool
        {
            get => m_OwnerPool;
            set => m_OwnerPool = value;
        }

        public bool IsSpawned
        {
            get => m_IsSpawned;
            set => m_IsSpawned = value;
        }
    }
}
