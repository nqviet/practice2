using System.Collections.Generic;
using UnityEngine;

namespace Game.Board
{
    /// <summary>
    /// Ordered list of levels played by the meta loop (scene_structure.md §6).
    /// PLACEHOLDER: holds direct LevelDefinition references. The Addressables-backed
    /// loader (levelIds + unlockRule, remote packs) replaces the direct list later
    /// without changing the Count / Get / IndexOf surface.
    /// </summary>
    [CreateAssetMenu(fileName = "LevelPack_01", menuName = "Block Breaker/Level Pack")]
    public class LevelPack : ScriptableObject
    {
        [SerializeField] private string m_PackId = "LevelPack_01";
        [SerializeField] private List<LevelDefinition> m_Levels = new List<LevelDefinition>();

        public string PackId
        {
            get => m_PackId;
            set => m_PackId = value;
        }

        public int Count => m_Levels != null ? m_Levels.Count : 0;

        public IReadOnlyList<LevelDefinition> Levels => m_Levels;

        public void SetLevels(IEnumerable<LevelDefinition> levels)
        {
            m_Levels = levels != null ? new List<LevelDefinition>(levels) : new List<LevelDefinition>();
        }

        public int ClampIndex(int index)
        {
            if (Count == 0) return 0;
            return Mathf.Clamp(index, 0, Count - 1);
        }

        /// <summary>Returns the level at a clamped index, or null when the pack is empty.</summary>
        public LevelDefinition Get(int index)
        {
            if (Count == 0) return null;
            return m_Levels[ClampIndex(index)];
        }

        public int IndexOf(LevelDefinition level)
        {
            return level != null && m_Levels != null ? m_Levels.IndexOf(level) : -1;
        }

        public bool HasNext(int index)
        {
            return index + 1 < Count;
        }
    }
}
