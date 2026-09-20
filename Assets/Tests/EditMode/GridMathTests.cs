using Game.Board;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class GridMathTests
    {
        [Test]
        public void CellToWorld_And_WorldToCell_RoundTrips()
        {
            Vector3 origin = new Vector3(-5f, 0f, 0f);
            Vector2Int originalCell = new Vector2Int(3, 7);

            Vector3 worldPos = GridMath.CellToWorld(originalCell, origin);
            Vector2Int roundTripCell = GridMath.WorldToCell(worldPos, origin);

            Assert.AreEqual(originalCell, roundTripCell);
        }

        [Test]
        public void TopRowFirst_Mapping_WorksCorrectly()
        {
            // Row 0 is at bottom (index 12 in 13-row grid), Row 12 is at top (index 0)
            int totalRows = 13;
            Assert.AreEqual(12, GridMath.RowFromTopIndex(0, totalRows));
            Assert.AreEqual(0, GridMath.RowFromTopIndex(12, totalRows));
            Assert.AreEqual(0, GridMath.TopIndexFromRow(12, totalRows));
            Assert.AreEqual(12, GridMath.TopIndexFromRow(0, totalRows));
        }

        [Test]
        public void InGrid_ValidatesBounds()
        {
            Assert.IsTrue(GridMath.InGrid(new Vector2Int(0, 0)));
            Assert.IsTrue(GridMath.InGrid(new Vector2Int(GameConstants.GridColumns - 1, GameConstants.GridRows - 1)));
            Assert.IsFalse(GridMath.InGrid(new Vector2Int(-1, 0)));
            Assert.IsFalse(GridMath.InGrid(new Vector2Int(0, -1)));
            Assert.IsFalse(GridMath.InGrid(new Vector2Int(GameConstants.GridColumns, 0)));
            Assert.IsFalse(GridMath.InGrid(new Vector2Int(0, GameConstants.GridRows)));
        }

        [Test]
        public void Neighbors4_DoesNotIncludeDiagonals_AndClipsAtBorders()
        {
            var cornerNeighbors = new System.Collections.Generic.List<Vector2Int>(GridMath.Neighbors4(new Vector2Int(0, 0)));
            Assert.AreEqual(2, cornerNeighbors.Count); // (1,0) and (0,1)

            var centerNeighbors = new System.Collections.Generic.List<Vector2Int>(GridMath.Neighbors4(new Vector2Int(5, 5)));
            Assert.AreEqual(4, centerNeighbors.Count);
        }

        [Test]
        public void TopRowFirst_AsymmetricFixture_RoundTrips()
        {
            Vector3 origin = new Vector3(-5f, 0f, 0f);

            // Asymmetric test point: Top row, index 0 -> row 12, col 7
            int topIndex = 0;
            int col = 7;
            int row = GridMath.RowFromTopIndex(topIndex, 13);
            Assert.AreEqual(12, row);

            Vector3 worldPos = GridMath.CellToWorld(new Vector2Int(col, row), origin);
            Assert.AreEqual(new Vector3(-5f + 7.5f, 12.5f, 0f), worldPos);

            Vector2Int roundTripCell = GridMath.WorldToCell(worldPos, origin);
            Assert.AreEqual(new Vector2Int(col, 12), roundTripCell);
            Assert.AreEqual(topIndex, GridMath.TopIndexFromRow(roundTripCell.y, 13));
        }
    }
}
