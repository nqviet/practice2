using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Board
{
    [CreateAssetMenu(fileName = "LevelDefinition_01", menuName = "Block Breaker/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string m_LevelId = "Level_01";
        [SerializeField] private string m_DisplayName = "LEVEL 1";
        [SerializeField] private int m_ParShots = 10;
        [SerializeField] private BlockPalette m_Palette;
        [TextArea(1, 1)]
        [SerializeField] private string[] m_Rows = new string[GameConstants.GridRows];

        // Derived / cached metadata
        [SerializeField] private List<GameColor> m_ColorsUsed = new List<GameColor>();
        [SerializeField] private int m_BrickCount;
        [SerializeField] private int m_SteelCount;
        [SerializeField] private int m_ColoredCount;
        [SerializeField] private int m_EmptyCount;

        [NonSerialized] private BoardState m_InitialState;

        public string LevelId
        {
            get => m_LevelId;
            set => m_LevelId = value;
        }

        public string DisplayName
        {
            get => m_DisplayName;
            set => m_DisplayName = value;
        }

        public int ParShots
        {
            get => m_ParShots;
            set => m_ParShots = value;
        }

        public BlockPalette Palette
        {
            get => m_Palette;
            set => m_Palette = value;
        }

        public string[] Rows
        {
            get => m_Rows;
            set => m_Rows = value;
        }

        public IReadOnlyList<GameColor> ColorsUsed => m_ColorsUsed;
        public int BrickCount => m_BrickCount;
        public int SteelCount => m_SteelCount;
        public int ColoredCount => m_ColoredCount;
        public int EmptyCount => m_EmptyCount;

        public BoardState InitialState
        {
            get => m_InitialState;
            set => m_InitialState = value;
        }

        public void SetDerivedMetadata(List<GameColor> colorsUsed, int brickCount, int steelCount, int coloredCount, int emptyCount)
        {
            m_ColorsUsed = colorsUsed != null ? new List<GameColor>(colorsUsed) : new List<GameColor>();
            m_BrickCount = brickCount;
            m_SteelCount = steelCount;
            m_ColoredCount = coloredCount;
            m_EmptyCount = emptyCount;
        }

        public BoardState Parse(out List<string> errors)
        {
            errors = new List<string>();
            var boardState = new BoardState();

            if (m_Rows == null || m_Rows.Length != GameConstants.GridRows)
            {
                int actualCount = m_Rows == null ? 0 : m_Rows.Length;
                errors.Add($"Level '{m_LevelId}' must have exactly {GameConstants.GridRows} rows (got {actualCount}).");
                return boardState;
            }

            var colorsFound = new HashSet<GameColor>();
            int bricks = 0;
            int steels = 0;
            int coloreds = 0;
            int empties = 0;

            for (int rowIndex = 0; rowIndex < m_Rows.Length; rowIndex++)
            {
                string rowString = m_Rows[rowIndex];
                int targetRow = GridMath.RowFromTopIndex(rowIndex, GameConstants.GridRows);

                if (string.IsNullOrEmpty(rowString) || rowString.Length != GameConstants.GridColumns)
                {
                    int actualLength = rowString == null ? 0 : rowString.Length;
                    errors.Add($"Row index {rowIndex} (target row {targetRow}) must have exactly {GameConstants.GridColumns} characters (got {actualLength}).");
                    continue;
                }

                for (int col = 0; col < rowString.Length; col++)
                {
                    char c = rowString[col];
                    Vector2Int cell = new Vector2Int(col, targetRow);

                    if (c == '.')
                    {
                        boardState.SetCell(cell, BlockKind.Empty, GameColor.None);
                        empties++;
                        continue;
                    }

                    BlockDefinition def = null;
                    string resolveError = null;

                    if (m_Palette != null)
                    {
                        if (!m_Palette.TryResolve(c, out def, out resolveError))
                        {
                            errors.Add($"Row {rowIndex} col {col} (cell {cell}): {resolveError}");
                            continue;
                        }
                    }
                    else
                    {
                        // Default built-in parsing if palette is not yet assigned
                        if (c == 'M' || c == 'X')
                        {
                            errors.Add($"Row {rowIndex} col {col} (cell {cell}): Character '{c}' is a reserved kind, not implemented.");
                            continue;
                        }

                        switch (c)
                        {
                            case 'N':
                                boardState.SetCell(cell, BlockKind.Brick, GameColor.None);
                                bricks++;
                                break;
                            case 'S':
                                boardState.SetCell(cell, BlockKind.Steel, GameColor.None);
                                steels++;
                                break;
                            case 'R':
                                boardState.SetCell(cell, BlockKind.Colored, GameColor.Red);
                                colorsFound.Add(GameColor.Red);
                                coloreds++;
                                break;
                            case 'B':
                                boardState.SetCell(cell, BlockKind.Colored, GameColor.Blue);
                                colorsFound.Add(GameColor.Blue);
                                coloreds++;
                                break;
                            case 'Y':
                                boardState.SetCell(cell, BlockKind.Colored, GameColor.Yellow);
                                colorsFound.Add(GameColor.Yellow);
                                coloreds++;
                                break;
                            default:
                                errors.Add($"Row {rowIndex} col {col} (cell {cell}): Unrecognized character '{c}'.");
                                break;
                        }
                        continue;
                    }

                    if (def != null)
                    {
                        boardState.SetCell(cell, def.Kind, def.Color);
                        if (def.Kind == BlockKind.Brick) bricks++;
                        else if (def.Kind == BlockKind.Steel) steels++;
                        else if (def.Kind == BlockKind.Colored)
                        {
                            coloreds++;
                            if (def.Color != GameColor.None)
                            {
                                colorsFound.Add(def.Color);
                            }
                        }
                    }
                    else
                    {
                        boardState.SetCell(cell, BlockKind.Empty, GameColor.None);
                        empties++;
                    }
                }
            }

            var colorsList = new List<GameColor>(colorsFound);
            colorsList.Sort();
            SetDerivedMetadata(colorsList, bricks, steels, coloreds, empties);

            m_InitialState = boardState.Clone();
            return boardState;
        }
    }
}
