using System.Collections.Generic;
using System.Text;
using Game.Core;
using UnityEngine;

namespace Game.Board
{
    public class ValidationReport
    {
        private readonly List<string> m_Errors = new List<string>();
        private readonly List<string> m_Warnings = new List<string>();
        private readonly List<Vector2Int> m_UncoveredBricks = new List<Vector2Int>();
        private readonly List<Vector2Int> m_UnreachableColored = new List<Vector2Int>();
        private readonly List<GameColor> m_ColorsUsed = new List<GameColor>();

        public IReadOnlyList<string> Errors => m_Errors;
        public IReadOnlyList<string> Warnings => m_Warnings;
        public IReadOnlyList<Vector2Int> UncoveredBricks => m_UncoveredBricks;
        public IReadOnlyList<Vector2Int> UnreachableColored => m_UnreachableColored;
        public IReadOnlyList<GameColor> ColorsUsed => m_ColorsUsed;

        public bool IsValid => m_Errors.Count == 0;

        public int BrickCount { get; set; }
        public int SteelCount { get; set; }
        public int ColoredCount { get; set; }
        public int RedCount { get; set; }
        public int BlueCount { get; set; }
        public int YellowCount { get; set; }
        public int EmptyCount { get; set; }

        public void AddError(string error) => m_Errors.Add(error);
        public void AddWarning(string warning) => m_Warnings.Add(warning);
        public void AddUncoveredBrick(Vector2Int cell) => m_UncoveredBricks.Add(cell);
        public void AddUnreachableColored(Vector2Int cell) => m_UnreachableColored.Add(cell);
        public void SetColorsUsed(IEnumerable<GameColor> colors)
        {
            m_ColorsUsed.Clear();
            if (colors != null) m_ColorsUsed.AddRange(colors);
        }

        public string GenerateSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[LevelValidator] Status: {(IsValid ? "VALID" : "INVALID")} ({m_Errors.Count} errors, {m_Warnings.Count} warnings)");
            sb.AppendLine($"Final Mix: {BrickCount} Bricks | {ColoredCount} Colored (R:{RedCount}, B:{BlueCount}, Y:{YellowCount}) | {SteelCount} Steel | {EmptyCount} Empty (Total: {BrickCount + ColoredCount + SteelCount + EmptyCount} cells)");
            sb.AppendLine($"Colors Used: {string.Join(", ", m_ColorsUsed)}");

            if (m_Errors.Count > 0)
            {
                sb.AppendLine("Errors:");
                for (int i = 0; i < m_Errors.Count; i++)
                {
                    sb.AppendLine($" - {m_Errors[i]}");
                }
            }

            return sb.ToString();
        }
    }

    public static class LevelValidator
    {
        public static ValidationReport Validate(LevelDefinition levelDef)
        {
            var report = new ValidationReport();

            if (levelDef == null)
            {
                report.AddError("LevelDefinition is null.");
                return report;
            }

            BoardState board = levelDef.Parse(out List<string> parseErrors);
            for (int i = 0; i < parseErrors.Count; i++)
            {
                report.AddError(parseErrors[i]);
            }

            // Count cell kinds and colors
            int brickCount = 0;
            int steelCount = 0;
            int coloredCount = 0;
            int redCount = 0;
            int blueCount = 0;
            int yellowCount = 0;
            int emptyCount = 0;

            var coloredCells = new List<Vector2Int>();
            var brickCells = new List<Vector2Int>();

            foreach (Vector2Int cell in board.Cells)
            {
                BlockKind kind = board.KindAt(cell);
                GameColor color = board.ColorAt(cell);

                switch (kind)
                {
                    case BlockKind.Brick:
                        brickCount++;
                        brickCells.Add(cell);
                        break;
                    case BlockKind.Steel:
                        steelCount++;
                        break;
                    case BlockKind.Colored:
                        coloredCount++;
                        coloredCells.Add(cell);
                        if (color == GameColor.Red) redCount++;
                        else if (color == GameColor.Blue) blueCount++;
                        else if (color == GameColor.Yellow) yellowCount++;
                        break;
                    case BlockKind.Empty:
                        emptyCount++;
                        break;
                }
            }

            report.BrickCount = brickCount;
            report.SteelCount = steelCount;
            report.ColoredCount = coloredCount;
            report.RedCount = redCount;
            report.BlueCount = blueCount;
            report.YellowCount = yellowCount;
            report.EmptyCount = emptyCount;
            report.SetColorsUsed(levelDef.ColorsUsed);

            if (brickCount == 0)
            {
                report.AddWarning("Level has no bricks (no objective).");
            }

            // 1. Coverage Check: Every brick must sit inside some colored block's 3x3 ring (Ring8)
            var coveredCells = new HashSet<Vector2Int>();
            for (int i = 0; i < coloredCells.Count; i++)
            {
                foreach (Vector2Int ringCell in GridMath.Ring8(coloredCells[i]))
                {
                    coveredCells.Add(ringCell);
                }
            }

            for (int i = 0; i < brickCells.Count; i++)
            {
                Vector2Int brick = brickCells[i];
                if (!coveredCells.Contains(brick))
                {
                    report.AddUncoveredBrick(brick);
                    report.AddError($"Coverage violation: Brick at ({brick.x}, {brick.y}) is outside all colored block 3x3 rings.");
                }
            }

            // 2. Access Check: 4-way BFS from landing lane (rows 0-1) across empty cells
            var reachableEmpty = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();

            for (int x = 0; x < GameConstants.GridColumns; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    Vector2Int laneCell = new Vector2Int(x, y);
                    if (board.KindAt(laneCell) == BlockKind.Empty)
                    {
                        reachableEmpty.Add(laneCell);
                        queue.Enqueue(laneCell);
                    }
                }
            }

            while (queue.Count > 0)
            {
                Vector2Int cur = queue.Dequeue();
                foreach (Vector2Int neighbor in GridMath.Neighbors4(cur))
                {
                    if (board.KindAt(neighbor) == BlockKind.Empty && !reachableEmpty.Contains(neighbor))
                    {
                        reachableEmpty.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // Direct reachability: Colored block touches a reachable empty cell 4-way
            var reachableColored = new HashSet<Vector2Int>();
            for (int i = 0; i < coloredCells.Count; i++)
            {
                Vector2Int c = coloredCells[i];
                foreach (Vector2Int neighbor in GridMath.Neighbors4(c))
                {
                    if (reachableEmpty.Contains(neighbor))
                    {
                        reachableColored.Add(c);
                        break;
                    }
                }
            }

            // 3. Chain Access Fallback (Fixpoint):
            // If a colored block is reachable and has an 8-way same-color neighbor, that neighbor is reachable too.
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < coloredCells.Count; i++)
                {
                    Vector2Int candidate = coloredCells[i];
                    if (reachableColored.Contains(candidate)) continue;

                    GameColor candidateColor = board.ColorAt(candidate);

                    foreach (Vector2Int ringNeighbor in GridMath.Ring8(candidate))
                    {
                        if (reachableColored.Contains(ringNeighbor) &&
                            board.KindAt(ringNeighbor) == BlockKind.Colored &&
                            board.ColorAt(ringNeighbor) == candidateColor)
                        {
                            reachableColored.Add(candidate);
                            changed = true;
                            break;
                        }
                    }
                }
            }

            // Report unreachable colored blocks
            for (int i = 0; i < coloredCells.Count; i++)
            {
                Vector2Int c = coloredCells[i];
                if (!reachableColored.Contains(c))
                {
                    report.AddUnreachableColored(c);
                    report.AddError($"Access violation: Colored block ({board.ColorAt(c)}) at ({c.x}, {c.y}) cannot be reached 4-way or chained.");
                }
            }

            return report;
        }

        public static bool ValidateBallClearance(float ballRadius, out string error)
        {
            // Ball clearance invariant: 2 * ballRadius < 1f - 0.1f (0.9f)
            float maxSafeDiameter = 0.9f;
            float diameter = 2f * ballRadius;
            if (ballRadius > 0.35f || diameter >= maxSafeDiameter)
            {
                error = $"Ball radius {ballRadius:F3} (diameter {diameter:F3}) violates clearance invariant (must be <= 0.35 and diameter < 0.90).";
                return false;
            }

            error = null;
            return true;
        }
    }
}
