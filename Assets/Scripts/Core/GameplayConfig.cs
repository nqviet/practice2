using System;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    public struct PoolWarmup
    {
        public PoolId Id;
        public int Count;

        public PoolWarmup(PoolId id, int count)
        {
            Id = id;
            Count = count;
        }
    }

    [CreateAssetMenu(fileName = "GameplayConfig", menuName = "Block Breaker/Gameplay Config")]
    public class GameplayConfig : ScriptableObject
    {
        [Header("Ball Invariants")]
        [Tooltip("Invariant <= 0.35: must pass 1-cell cracks with clearance")]
        [SerializeField] private float m_BallRadius = 0.28f;
        [SerializeField] private float m_BallSpeed = 14.0f;
        [SerializeField] private float m_FastForwardMultiplier = 2.0f;
        [SerializeField] private float m_AimClampFromUpDeg = 75.0f;

        [Header("Comet Trail Settings")]
        [SerializeField] private int m_MaxTrailRuns = 5;
        [SerializeField] private float m_TrailRunFadeSec = 0.25f;
        [SerializeField] private float m_TrailDissipateSec = 1.0f;

        [Header("Gameplay Timing & Margins")]
        [SerializeField] private float m_ChainStaggerSec = 0.05f;
        [SerializeField] private float m_DetonationFreezeSec = 0.04f;
        [SerializeField] private float m_ReturnMargin = 0.35f;
        [SerializeField] private float m_ShotTimeoutSec = 12.0f;

        [Header("Arena & Cannon Layout")]
        [SerializeField] private float m_CannonRailHalfWidth = 4.4f;
        [SerializeField] private float m_CannonBottomPad = 1.8f; // banner → cannon pivot: BottomBar tray (1.2 u) + half the cannon base + a gap
        [SerializeField] private float m_ShakeScale = 1.0f;

        [Header("Pool Warmup")]
        [SerializeField] private PoolWarmup[] m_Warmups = new[]
        {
            new PoolWarmup(PoolId.Ball, 1),
            new PoolWarmup(PoolId.TrailRun, 5),
            new PoolWarmup(PoolId.Block, 64)
        };

        public float BallRadius
        {
            get => m_BallRadius;
            set => m_BallRadius = value;
        }

        public float BallSpeed
        {
            get => m_BallSpeed;
            set => m_BallSpeed = value;
        }

        public float FastForwardMultiplier
        {
            get => m_FastForwardMultiplier;
            set => m_FastForwardMultiplier = value;
        }

        public float AimClampFromUpDeg
        {
            get => m_AimClampFromUpDeg;
            set => m_AimClampFromUpDeg = value;
        }

        public int MaxTrailRuns
        {
            get => m_MaxTrailRuns;
            set => m_MaxTrailRuns = value;
        }

        public float TrailRunFadeSec
        {
            get => m_TrailRunFadeSec;
            set => m_TrailRunFadeSec = value;
        }

        public float TrailDissipateSec
        {
            get => m_TrailDissipateSec;
            set => m_TrailDissipateSec = value;
        }

        public float ChainStaggerSec
        {
            get => m_ChainStaggerSec;
            set => m_ChainStaggerSec = value;
        }

        public float DetonationFreezeSec
        {
            get => m_DetonationFreezeSec;
            set => m_DetonationFreezeSec = value;
        }

        public float ReturnMargin
        {
            get => m_ReturnMargin;
            set => m_ReturnMargin = value;
        }

        public float ShotTimeoutSec
        {
            get => m_ShotTimeoutSec;
            set => m_ShotTimeoutSec = value;
        }

        public float CannonRailHalfWidth
        {
            get => m_CannonRailHalfWidth;
            set => m_CannonRailHalfWidth = value;
        }

        public float CannonBottomPad
        {
            get => m_CannonBottomPad;
            set => m_CannonBottomPad = value;
        }

        public float ShakeScale
        {
            get => m_ShakeScale;
            set => m_ShakeScale = value;
        }

        public PoolWarmup[] Warmups
        {
            get => m_Warmups;
            set => m_Warmups = value;
        }
    }
}
