using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Board
{
    [Serializable]
    public struct PaletteEntry
    {
        [SerializeField] private char m_Character;
        [SerializeField] private BlockDefinition m_Definition;
        [SerializeField] private bool m_IsReserved;
        [SerializeField] private string m_ReservedMessage;

        public char Character
        {
            get => m_Character;
            set => m_Character = value;
        }

        public BlockDefinition Definition
        {
            get => m_Definition;
            set => m_Definition = value;
        }

        public bool IsReserved
        {
            get => m_IsReserved;
            set => m_IsReserved = value;
        }

        public string ReservedMessage
        {
            get => m_ReservedMessage;
            set => m_ReservedMessage = value;
        }

        public PaletteEntry(char character, BlockDefinition definition, bool isReserved = false, string reservedMessage = null)
        {
            m_Character = character;
            m_Definition = definition;
            m_IsReserved = isReserved;
            m_ReservedMessage = reservedMessage;
        }
    }

    [CreateAssetMenu(fileName = "BlockPalette", menuName = "Block Breaker/Block Palette")]
    public class BlockPalette : ScriptableObject
    {
        [SerializeField] private List<PaletteEntry> m_Entries = new List<PaletteEntry>();

        public IReadOnlyList<PaletteEntry> Entries => m_Entries;

        public void AddOrUpdateEntry(PaletteEntry entry)
        {
            for (int i = 0; i < m_Entries.Count; i++)
            {
                if (m_Entries[i].Character == entry.Character)
                {
                    m_Entries[i] = entry;
                    return;
                }
            }
            m_Entries.Add(entry);
        }

        public bool TryResolve(char c, out BlockDefinition definition, out string errorMessage)
        {
            definition = null;
            errorMessage = null;

            if (c == '.')
            {
                // '.' is standard empty cell
                return true;
            }

            for (int i = 0; i < m_Entries.Count; i++)
            {
                PaletteEntry entry = m_Entries[i];
                if (entry.Character == c)
                {
                    if (entry.IsReserved)
                    {
                        errorMessage = !string.IsNullOrEmpty(entry.ReservedMessage)
                            ? entry.ReservedMessage
                            : $"Character '{c}' is reserved and not implemented.";
                        return false;
                    }

                    definition = entry.Definition;
                    return true;
                }
            }

            // Standard fallback checks if not explicitly configured in entries
            if (c == 'M' || c == 'X')
            {
                errorMessage = $"Character '{c}' is a reserved kind, not implemented.";
                return false;
            }

            errorMessage = $"Unrecognized character '{c}'.";
            return false;
        }
    }
}
