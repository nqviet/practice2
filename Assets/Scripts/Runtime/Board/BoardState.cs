using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Board
{
    public class BoardState
    {
        private readonly BlockKind[,] m_Kinds;
        private readonly GameColor[,] m_Colors;
        private int m_BricksRemaining;

        public int BricksRemaining => m_BricksRemaining;
        public int Columns => GameConstants.GridColumns;
        public int Rows => GameConstants.GridRows;

        public BoardState()
        {
            m_Kinds = new BlockKind[Columns, Rows];
            m_Colors = new GameColor[Columns, Rows];
            m_BricksRemaining = 0;
        }

        public BlockKind KindAt(Vector2Int cell)
        {
            if (!GridMath.InGrid(cell)) return BlockKind.Empty;
            return m_Kinds[cell.x, cell.y];
        }

        public GameColor ColorAt(Vector2Int cell)
        {
            if (!GridMath.InGrid(cell)) return GameColor.None;
            return m_Colors[cell.x, cell.y];
        }

        public void SetCell(Vector2Int cell, BlockKind kind, GameColor color)
        {
            if (!GridMath.InGrid(cell)) return;

            BlockKind prevKind = m_Kinds[cell.x, cell.y];
            if (prevKind == BlockKind.Brick) m_BricksRemaining--;
            if (kind == BlockKind.Brick) m_BricksRemaining++;

            m_Kinds[cell.x, cell.y] = kind;
            m_Colors[cell.x, cell.y] = color;
        }

        public void ClearCell(Vector2Int cell)
        {
            SetCell(cell, BlockKind.Empty, GameColor.None);
        }

        public BoardState Clone()
        {
            var clone = new BoardState();
            foreach (var cell in Cells)
            {
                clone.SetCell(cell, KindAt(cell), ColorAt(cell));
            }
            return clone;
        }

        public int CountOfKind(BlockKind kind)
        {
            int count = 0;
            foreach (var cell in Cells)
            {
                if (KindAt(cell) == kind) count++;
            }
            return count;
        }

        public int CountOfColor(GameColor color)
        {
            int count = 0;
            foreach (var cell in Cells)
            {
                if (ColorAt(cell) == color) count++;
            }
            return count;
        }

        public IEnumerable<Vector2Int> Cells
        {
            get
            {
                for (int x = 0; x < Columns; x++)
                {
                    for (int y = 0; y < Rows; y++)
                    {
                        yield return new Vector2Int(x, y);
                    }
                }
            }
        }
    }
}
