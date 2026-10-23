using System;
using NUnit.Framework;

namespace Flowers.Board.Tests
{
    public class BoardGeneratorTests
    {
        [Test]
        public void GeneratedBoards_AreFull_HaveNoMatches_AndHaveAMove()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var board = BoardGenerator.Generate(7, 9, 5, new Random(seed));

                Assert.That(board.Width, Is.EqualTo(7));
                Assert.That(board.Height, Is.EqualTo(9));
                TestBoards.AssertPlayable(board);
            }
        }

        [TestCase(5, 5, 3)]
        [TestCase(8, 8, 4)]
        [TestCase(6, 10, 5)]
        [TestCase(9, 11, 6)]
        public void OtherSizesAndTypeCounts_AlsoWork(int width, int height, int typeCount)
        {
            for (int seed = 0; seed < 50; seed++)
                TestBoards.AssertPlayable(BoardGenerator.Generate(width, height, typeCount, new Random(seed)));
        }

        [Test]
        public void SameSeed_GivesSameBoard()
        {
            var a = BoardGenerator.Generate(7, 9, 5, new Random(42));
            var b = BoardGenerator.Generate(7, 9, 5, new Random(42));

            Assert.That(a.ToString(), Is.EqualTo(b.ToString()));
        }

        [Test]
        public void UsesAllFlowerTypes_OnANormalBoard()
        {
            var board = BoardGenerator.Generate(7, 9, 5, new Random(1));

            Assert.That(TestBoards.CountTypes(board).Keys, Is.EquivalentTo(new[] { 0, 1, 2, 3, 4 }));
        }

        [Test]
        public void FewerThanThreeTypes_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => BoardGenerator.Generate(7, 9, 2, new Random(0)));
        }
    }
}
