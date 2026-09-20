using System.Collections.Generic;
using Game.Board;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    [TestFixture]
    public class ChainSolverTests
    {
        [Test]
        public void ChainSolver_BallMismatch_YieldsEmptyResult()
        {
            var board = new BoardState();
            Vector2Int seed = new Vector2Int(5, 5);
            board.SetCell(seed, BlockKind.Colored, GameColor.Red);

            ChainResult result = ChainSolver.Solve(board, seed, GameColor.Blue);
            Assert.AreEqual(0, result.DestroyedCells.Count);
            Assert.AreEqual(0, result.BricksDestroyed);
            Assert.AreEqual(0, result.ColoredDestroyed);
        }

        [Test]
        public void ChainSolver_WildcardBall_DetonatesAnyColor()
        {
            var board = new BoardState();
            Vector2Int seed = new Vector2Int(5, 5);
            board.SetCell(seed, BlockKind.Colored, GameColor.Yellow);
            board.SetCell(new Vector2Int(5, 6), BlockKind.Brick, GameColor.None);

            ChainResult result = ChainSolver.Solve(board, seed, GameColor.White);
            Assert.AreEqual(2, result.DestroyedCells.Count);
            Assert.AreEqual(1, result.ColoredDestroyed);
            Assert.AreEqual(1, result.BricksDestroyed);
            CollectionAssert.Contains(result.DestroyedCells, seed);
            CollectionAssert.Contains(result.DestroyedCells, new Vector2Int(5, 6));
        }

        [Test]
        public void ChainSolver_DifferentColoredBlock_InRing_Survives()
        {
            var board = new BoardState();
            Vector2Int seed = new Vector2Int(5, 5);
            board.SetCell(seed, BlockKind.Colored, GameColor.Red);

            Vector2Int blueNeighbor = new Vector2Int(6, 5);
            board.SetCell(blueNeighbor, BlockKind.Colored, GameColor.Blue);

            Vector2Int brickNeighbor = new Vector2Int(5, 6);
            board.SetCell(brickNeighbor, BlockKind.Brick, GameColor.None);

            ChainResult result = ChainSolver.Solve(board, seed, GameColor.Red);

            // Red block and Brick are destroyed, Blue block SURVIVES
            CollectionAssert.Contains(result.DestroyedCells, seed);
            CollectionAssert.Contains(result.DestroyedCells, brickNeighbor);
            CollectionAssert.DoesNotContain(result.DestroyedCells, blueNeighbor);
            Assert.AreEqual(1, result.BricksDestroyed);
            Assert.AreEqual(1, result.ColoredDestroyed);
        }

        [Test]
        public void ChainSolver_SteelAndEmpty_AreInert()
        {
            var board = new BoardState();
            Vector2Int seed = new Vector2Int(5, 5);
            board.SetCell(seed, BlockKind.Colored, GameColor.Blue);

            Vector2Int steelPos = new Vector2Int(6, 5);
            board.SetCell(steelPos, BlockKind.Steel, GameColor.None);

            Vector2Int emptyPos = new Vector2Int(5, 6);
            board.SetCell(emptyPos, BlockKind.Empty, GameColor.None);

            ChainResult result = ChainSolver.Solve(board, seed, GameColor.Blue);

            Assert.AreEqual(1, result.DestroyedCells.Count);
            Assert.AreEqual(seed, result.DestroyedCells[0]);
            CollectionAssert.DoesNotContain(result.DestroyedCells, steelPos);
            CollectionAssert.DoesNotContain(result.DestroyedCells, emptyPos);
        }

        [Test]
        public void ChainSolver_OverlappingRings_DestroyEachCellAtMostOnce()
        {
            var board = new BoardState();
            Vector2Int red1 = new Vector2Int(4, 5);
            Vector2Int red2 = new Vector2Int(5, 5);
            board.SetCell(red1, BlockKind.Colored, GameColor.Red);
            board.SetCell(red2, BlockKind.Colored, GameColor.Red);

            // Shared neighbor brick
            Vector2Int sharedBrick = new Vector2Int(4, 6);
            board.SetCell(sharedBrick, BlockKind.Brick, GameColor.None);

            ChainResult result = ChainSolver.Solve(board, red1, GameColor.Red);

            // Verify shared brick appears exactly once in DestroyedCells
            int occurrences = 0;
            for (int i = 0; i < result.DestroyedCells.Count; i++)
            {
                if (result.DestroyedCells[i] == sharedBrick) occurrences++;
            }
            Assert.AreEqual(1, occurrences);
        }

        [Test]
        public void ChainSolver_AdjacentSameColor_ChainsWithIncreasingDepth()
        {
            var board = new BoardState();
            Vector2Int red1 = new Vector2Int(3, 5);
            Vector2Int red2 = new Vector2Int(4, 5);
            Vector2Int brickDepth0 = new Vector2Int(2, 5);
            Vector2Int brickDepth1 = new Vector2Int(5, 5);

            board.SetCell(red1, BlockKind.Colored, GameColor.Red);
            board.SetCell(red2, BlockKind.Colored, GameColor.Red);
            board.SetCell(brickDepth0, BlockKind.Brick, GameColor.None);
            board.SetCell(brickDepth1, BlockKind.Brick, GameColor.None);

            ChainResult result = ChainSolver.Solve(board, red1, GameColor.Red);

            Assert.AreEqual(2, result.ColoredDestroyed);
            Assert.AreEqual(2, result.BricksDestroyed);

            Assert.AreEqual(0, result.DepthOf[red1]);
            Assert.AreEqual(0, result.DepthOf[brickDepth0]);
            Assert.AreEqual(1, result.DepthOf[red2]);
            Assert.AreEqual(1, result.DepthOf[brickDepth1]);
        }
    }
}
