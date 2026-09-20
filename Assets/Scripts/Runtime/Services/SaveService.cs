using System;
using System.IO;
using Game.Core;
using UnityEngine;

namespace Game.Runtime.Services
{
    /// <summary>
    /// JSON persistence at Application.persistentDataPath (technical_stack.md, technical_design.md §1.3).
    /// Plain C# so EditMode tests can point it at a temp file, or pass a null path for the
    /// in-memory fake. Writes go to a temp file first so a crash mid-write never corrupts the save.
    /// </summary>
    public class SaveService : ISaveService
    {
        private readonly string m_FilePath;
        private PlayerData m_Data = new PlayerData();

        public PlayerData Data => m_Data;
        public string FilePath => m_FilePath;
        public bool IsPersistent => !string.IsNullOrEmpty(m_FilePath);

        public int HighestUnlockedLevel
        {
            get => m_Data.HighestUnlockedIndex;
            set => m_Data.HighestUnlockedIndex = value;
        }

        public int CurrentLevelIndex
        {
            get => m_Data.CurrentLevelIndex;
            set => m_Data.CurrentLevelIndex = value;
        }

        public bool AdsRemoved
        {
            get => m_Data.AdsRemoved;
            set => m_Data.AdsRemoved = value;
        }

        /// <param name="filePath">Absolute save path, or null/empty for an in-memory store.</param>
        public SaveService(string filePath)
        {
            m_FilePath = filePath;
        }

        public static string DefaultFilePath => Path.Combine(Application.persistentDataPath, GameConstants.SaveFileName);

        public void Load()
        {
            if (!IsPersistent || !File.Exists(m_FilePath))
            {
                m_Data = new PlayerData();
                return;
            }

            try
            {
                string json = File.ReadAllText(m_FilePath);
                PlayerData loaded = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<PlayerData>(json);
                m_Data = loaded ?? new PlayerData();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Save file unreadable, starting fresh: {e.Message}");
                BackupCorruptFile();
                m_Data = new PlayerData();
            }

            m_Data.Sanitize();
        }

        public void Save()
        {
            if (!IsPersistent) return;

            string tempPath = m_FilePath + ".tmp";
            try
            {
                string directory = Path.GetDirectoryName(m_FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(tempPath, JsonUtility.ToJson(m_Data, true));
                File.Copy(tempPath, m_FilePath, true);
                File.Delete(tempPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] Failed to write save file '{m_FilePath}': {e.Message}");
            }
        }

        public void ResetData()
        {
            m_Data = new PlayerData();
            Save();
        }

        private void BackupCorruptFile()
        {
            try
            {
                File.Copy(m_FilePath, m_FilePath + ".corrupt", true);
            }
            catch (Exception)
            {
                // Best effort only — losing the backup must never block boot.
            }
        }
    }
}
