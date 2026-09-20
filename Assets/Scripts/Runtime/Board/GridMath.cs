using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Board
{
    public static class GridMath
    {
        public static Vector3 CellToWorld(Vector2Int cell, Vector3 gridOrigin)
        {
            return gridOrigin + new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
        }

        public static Vector2Int WorldToCell(Vector3 world, Vector3 gridOrigin)
        {
            Vector3 rel = world - gridOrigin;
            int col = Mathf.FloorToInt(rel.x);
            int row = Mathf.FloorToInt(rel.y);
            return new Vector2Int(col, row);
        }

        public static bool InGrid(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < GameConstants.GridColumns &&
                   cell.y >= 0 && cell.y < GameConstants.GridRows;
        }

        public static int RowFromTopIndex(int topIndex, int totalRows = GameConstants.GridRows)
        {
            return totalRows - 1 - topIndex;
        }

        public static int TopIndexFromRow(int row, int totalRows = GameConstants.GridRows)
        {
            return totalRows - 1 - row;
        }

        public static IEnumerable<Vector2Int> Neighbors4(Vector2Int center)
        {
            Vector2Int[] offsets = { new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int neighbor = center + offsets[i];
                if (InGrid(neighbor))
                {
                    yield return neighbor;
                }
            }
        }

        public static IEnumerable<Vector2Int> Ring8(Vector2Int center)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Vector2Int neighbor = new Vector2Int(center.x + dx, center.y + dy);
                    if (InGrid(neighbor))
                    {
                        yield return neighbor;
                    }
                }
            }
        }
    }
}
