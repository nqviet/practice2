using System.Collections.Generic;
using Game.Core;
using Game.Runtime.Services;
using UnityEngine;

namespace Game.Runtime.Ball
{
    public class BallTrail : MonoBehaviour
    {
        [SerializeField] private TrailRun m_TrailRunPrefab;
        [SerializeField] private int m_MaxRuns = 5;
        [SerializeField] private float m_FadeDuration = 0.25f;
        [SerializeField] private float m_DissipateDuration = 1.0f;

        private readonly Queue<TrailRun> m_ActiveRuns = new Queue<TrailRun>();
        private TrailRun m_CurrentRun;
        private Color m_TrailColor = Color.white;
        private Gradient m_TrailGradient;
        private PoolService m_PoolService;
        private bool m_IsActive;

        public int ActiveRunCount => m_ActiveRuns.Count;
        public TrailRun CurrentRun => m_CurrentRun;

        public void Configure(TrailRun prefab, int maxRuns, float fadeSec, float dissipateSec, PoolService poolService = null)
        {
            m_TrailRunPrefab = prefab;
            m_MaxRuns = maxRuns > 0 ? maxRuns : 5;
            m_FadeDuration = fadeSec;
            m_DissipateDuration = dissipateSec;
            m_PoolService = poolService != null ? poolService : PoolService.Instance;
        }

        public void Begin(Vector2 startPos, GameColor color, BallSkin skin = null)
        {
            EndShot();

            m_TrailColor = GetColorForGameColor(color);
            m_TrailGradient = skin != null ? skin.TrailGradient : null;
            if (m_PoolService == null)
            {
                m_PoolService = PoolService.Instance;
            }

            m_CurrentRun = SpawnRun(startPos);
            if (m_CurrentRun != null)
            {
                m_CurrentRun.Initialize(startPos, m_TrailColor, m_TrailGradient);
                m_ActiveRuns.Enqueue(m_CurrentRun);
            }

            m_IsActive = true;
            UpdateAgeWeights();
        }

        public void OnBounce(Vector2 contactPoint)
        {
            if (!m_IsActive) return;

            if (m_CurrentRun != null)
            {
                m_CurrentRun.UpdateHead(contactPoint);
            }

            if (m_ActiveRuns.Count >= m_MaxRuns)
            {
                TrailRun oldest = m_ActiveRuns.Dequeue();
                if (oldest != null)
                {
                    oldest.FadeAndDespawn(m_FadeDuration, m_PoolService);
                }
            }

            TrailRun nextRun = SpawnRun(contactPoint);
            if (nextRun != null)
            {
                nextRun.Initialize(contactPoint, m_TrailColor, m_TrailGradient);
                m_CurrentRun = nextRun;
                m_ActiveRuns.Enqueue(m_CurrentRun);
            }

            UpdateAgeWeights();
        }

        public void EndShot()
        {
            m_IsActive = false;

            while (m_ActiveRuns.Count > 0)
            {
                TrailRun run = m_ActiveRuns.Dequeue();
                if (run != null)
                {
                    run.Dissipate(m_DissipateDuration, m_PoolService);
                }
            }

            m_CurrentRun = null;
        }

        private void LateUpdate()
        {
            if (!m_IsActive || m_CurrentRun == null) return;

            m_CurrentRun.UpdateHead(transform.position);
            UpdateAgeWeights();
        }

        private void UpdateAgeWeights()
        {
            if (m_ActiveRuns.Count == 0) return;

            int count = m_ActiveRuns.Count;
            int index = 0;
            foreach (TrailRun run in m_ActiveRuns)
            {
                if (run != null)
                {
                    // Oldest run gets lower weight, head run gets 1.0f
                    float weight = Mathf.Lerp(0.35f, 1.0f, (float)(index + 1) / count);
                    run.SetAgeWeight(weight);
                }
                index++;
            }
        }

        private TrailRun SpawnRun(Vector2 pos)
        {
            if (m_PoolService != null && m_TrailRunPrefab != null)
            {
                return m_PoolService.Spawn(m_TrailRunPrefab, pos, Quaternion.identity);
            }
            else if (PoolService.HasInstance && m_TrailRunPrefab != null)
            {
                return PoolService.Instance.Spawn(m_TrailRunPrefab, pos, Quaternion.identity);
            }
            else if (m_TrailRunPrefab != null)
            {
                GameObject go = Instantiate(m_TrailRunPrefab.gameObject, pos, Quaternion.identity);
                return go.GetComponent<TrailRun>();
            }
            else
            {
                // Procedural fallback if no prefab wired
                var go = new GameObject("TrailRun_Procedural");
                go.transform.position = pos;
                var lr = go.AddComponent<LineRenderer>();
                lr.sortingLayerName = GameConstants.SortingBallTrail;
                var run = go.AddComponent<TrailRun>();
                return run;
            }
        }

        private static Color GetColorForGameColor(GameColor color)
        {
            switch (color)
            {
                case GameColor.Red: return new Color(1f, 0.3f, 0.32f);
                case GameColor.Blue: return new Color(0.28f, 0.6f, 1f);
                case GameColor.Yellow: return new Color(1f, 0.85f, 0.2f);
                case GameColor.White: return Color.white;
                default: return Color.white;
            }
        }

        private void OnDestroy()
        {
            EndShot();
        }
    }
}
