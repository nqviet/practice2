using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    public class LevelRecord
    {
        [SerializeField] private string m_LevelId;
        [SerializeField] private int m_BestShots;
        [SerializeField] private int m_Completions;

        public string LevelId => m_LevelId;
        public int BestShots => m_BestShots;
        public int Completions => m_Completions;

        public LevelRecord() { }

        public LevelRecord(string levelId)
        {
            m_LevelId = levelId;
        }

        /// <summary>Registers a completion; returns true when shots beat the stored best.</summary>
        public bool RegisterCompletion(int shots)
        {
            m_Completions++;
            if (m_BestShots <= 0 || shots < m_BestShots)
            {
                m_BestShots = shots;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Everything persisted to persistentDataPath as JSON (technical_design.md §1.3, §2).
    /// Progress + settings only — no currency, no rewards (gameplay.md §9 decision 2).
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        [SerializeField] private int m_Version = GameConstants.SaveVersion;

        [Header("Progress")]
        [SerializeField] private int m_CurrentLevelIndex;
        [SerializeField] private int m_HighestUnlockedIndex;
        [SerializeField] private List<LevelRecord> m_LevelRecords = new List<LevelRecord>();

        [Header("Monetization")]
        [SerializeField] private bool m_AdsRemoved;

        [Header("Settings")]
        [SerializeField] private bool m_MusicEnabled = true;
        [SerializeField] private bool m_SfxEnabled = true;
        [SerializeField] private float m_MusicVolume = 0.7f;
        [SerializeField] private float m_SfxVolume = 1.0f;

        public int Version
        {
            get => m_Version;
            set => m_Version = value;
        }

        public int CurrentLevelIndex
        {
            get => m_CurrentLevelIndex;
            set => m_CurrentLevelIndex = Mathf.Max(0, value);
        }

        public int HighestUnlockedIndex
        {
            get => m_HighestUnlockedIndex;
            set => m_HighestUnlockedIndex = Mathf.Max(0, value);
        }

        public bool AdsRemoved
        {
            get => m_AdsRemoved;
            set => m_AdsRemoved = value;
        }

        public bool MusicEnabled
        {
            get => m_MusicEnabled;
            set => m_MusicEnabled = value;
        }

        public bool SfxEnabled
        {
            get => m_SfxEnabled;
            set => m_SfxEnabled = value;
        }

        public float MusicVolume
        {
            get => m_MusicVolume;
            set => m_MusicVolume = Mathf.Clamp01(value);
        }

        public float SfxVolume
        {
            get => m_SfxVolume;
            set => m_SfxVolume = Mathf.Clamp01(value);
        }

        /// <summary>Volume actually applied to the music bus (toggle × slider).</summary>
        public float EffectiveMusicVolume => m_MusicEnabled ? m_MusicVolume : 0f;

        /// <summary>Volume actually applied to the SFX/UI buses (toggle × slider).</summary>
        public float EffectiveSfxVolume => m_SfxEnabled ? m_SfxVolume : 0f;

        public IReadOnlyList<LevelRecord> LevelRecords => m_LevelRecords;

        public LevelRecord GetRecord(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || m_LevelRecords == null) return null;

            for (int i = 0; i < m_LevelRecords.Count; i++)
            {
                if (m_LevelRecords[i] != null && m_LevelRecords[i].LevelId == levelId)
                {
                    return m_LevelRecords[i];
                }
            }
            return null;
        }

        public LevelRecord GetOrCreateRecord(string levelId)
        {
            LevelRecord record = GetRecord(levelId);
            if (record == null)
            {
                if (m_LevelRecords == null) m_LevelRecords = new List<LevelRecord>();
                record = new LevelRecord(levelId);
                m_LevelRecords.Add(record);
            }
            return record;
        }

        /// <summary>Repairs data loaded from an older or hand-edited file.</summary>
        public void Sanitize()
        {
            if (m_LevelRecords == null) m_LevelRecords = new List<LevelRecord>();
            m_LevelRecords.RemoveAll(r => r == null || string.IsNullOrEmpty(r.LevelId));

            m_CurrentLevelIndex = Mathf.Max(0, m_CurrentLevelIndex);
            m_HighestUnlockedIndex = Mathf.Max(0, m_HighestUnlockedIndex);
            m_MusicVolume = Mathf.Clamp01(m_MusicVolume);
            m_SfxVolume = Mathf.Clamp01(m_SfxVolume);
            m_Version = GameConstants.SaveVersion;
        }
    }
}
