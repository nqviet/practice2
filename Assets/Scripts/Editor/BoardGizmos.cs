using Game.Board;
using Game.Core;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class BoardGizmos
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
        private static void DrawLevelControllerGizmos(LevelController controller, GizmoType gizmoType)
        {
            if (controller == null) return;

            Vector3 origin = controller.GridOrigin != null
                ? controller.GridOrigin.position
                : new Vector3(GameConstants.GridOriginX, GameConstants.GridOriginY, 0f);

            // Draw Return Line
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            float lineY = origin.y + GameConstants.ReturnLineY;
            float leftX = origin.x;
            float rightX = origin.x + (GameConstants.GridColumns * GameConstants.CellSize);
            Gizmos.DrawLine(new Vector3(leftX - 0.5f, lineY, 0f), new Vector3(rightX + 0.5f, lineY, 0f));

            // Draw Landing Lane (Rows 0 and 1)
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
            Vector3 laneCenter = origin + new Vector3((GameConstants.GridColumns * GameConstants.CellSize) * 0.5f, 1.0f, 0f);
            Vector3 laneSize = new Vector3(GameConstants.GridColumns * GameConstants.CellSize, 2.0f, 0f);
            Gizmos.DrawWireCube(laneCenter, laneSize);

            // Draw Grid Outline
            Gizmos.color = new Color(1f, 1f, 1f, 0.4f);
            Vector3 gridCenter = origin + new Vector3(
                (GameConstants.GridColumns * GameConstants.CellSize) * 0.5f,
                (GameConstants.GridRows * GameConstants.CellSize) * 0.5f,
                0f);
            Vector3 gridSize = new Vector3(
                GameConstants.GridColumns * GameConstants.CellSize,
                GameConstants.GridRows * GameConstants.CellSize,
                0f);
            Gizmos.DrawWireCube(gridCenter, gridSize);

            // Draw Cell Slots
            Gizmos.color = new Color(1f, 1f, 1f, 0.1f);
            for (int col = 0; col < GameConstants.GridColumns; col++)
            {
                for (int row = 0; row < GameConstants.GridRows; row++)
                {
                    Vector3 cellWorld = GridMath.CellToWorld(new Vector2Int(col, row), origin);
                    Gizmos.DrawWireCube(cellWorld, new Vector3(0.95f, 0.95f, 0f));
                }
            }

            // If selected, draw 3x3 rings around colored blocks
            if ((gizmoType & GizmoType.Selected) != 0 && controller.State != null)
            {
                foreach (Vector2Int cell in controller.State.Cells)
                {
                    if (controller.State.KindAt(cell) == BlockKind.Colored)
                    {
                        GameColor color = controller.State.ColorAt(cell);
                        Color ringColor = Color.white;
                        if (color == GameColor.Red) ringColor = new Color(1f, 0.2f, 0.2f, 0.4f);
                        else if (color == GameColor.Blue) ringColor = new Color(0.2f, 0.5f, 1f, 0.4f);
                        else if (color == GameColor.Yellow) ringColor = new Color(1f, 0.9f, 0.2f, 0.4f);

                        Gizmos.color = ringColor;
                        Vector3 center = GridMath.CellToWorld(cell, origin);
                        Gizmos.DrawWireCube(center, new Vector3(3f, 3f, 0f));
                    }
                }
            }
        }
    }
}
