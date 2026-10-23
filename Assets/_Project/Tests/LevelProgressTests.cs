using System.Collections.Generic;
using Flowers.Levels;
using NUnit.Framework;

namespace Flowers.Board.Tests
{
    public class LevelProgressTests
    {
        private static readonly string[] Ids = { "L1", "L2", "L3", "L4" };

        [Test]
        public void FreshProgress_OnlyFirstLevelIsOpen()
        {
            var progress = new LevelProgress();

            Assert.That(progress.GetStatus(Ids, 0), Is.EqualTo(LevelStatus.Open));
            Assert.That(progress.GetStatus(Ids, 1), Is.EqualTo(LevelStatus.Locked));
            Assert.That(progress.GetStatus(Ids, 3), Is.EqualTo(LevelStatus.Locked));
            Assert.That(progress.CurrentIndex(Ids), Is.EqualTo(0));
            Assert.That(progress.TotalStars, Is.EqualTo(0));
        }

        [Test]
        public void WinningALevel_CompletesIt_AndOpensTheNext()
        {
            var progress = new LevelProgress();

            progress.RecordWin("L1", 2);

            Assert.That(progress.GetStatus(Ids, 0), Is.EqualTo(LevelStatus.Completed));
            Assert.That(progress.GetStatus(Ids, 1), Is.EqualTo(LevelStatus.Open));
            Assert.That(progress.GetStatus(Ids, 2), Is.EqualTo(LevelStatus.Locked));
            Assert.That(progress.CurrentIndex(Ids), Is.EqualTo(1));
        }

        [Test]
        public void BestStarsAreKept()
        {
            var progress = new LevelProgress();

            Assert.That(progress.RecordWin("L1", 2), Is.True, "first win is a new best");
            Assert.That(progress.RecordWin("L1", 1), Is.False, "worse result");
            Assert.That(progress.GetStars("L1"), Is.EqualTo(2));
            Assert.That(progress.RecordWin("L1", 3), Is.True, "better result");
            Assert.That(progress.GetStars("L1"), Is.EqualTo(3));
        }

        [Test]
        public void StarsAreClampedTo1To3()
        {
            var progress = new LevelProgress();

            progress.RecordWin("L1", 9);
            progress.RecordWin("L2", 0);

            Assert.That(progress.GetStars("L1"), Is.EqualTo(3));
            Assert.That(progress.GetStars("L2"), Is.EqualTo(1), "a win is always at least 1 star");
        }

        [Test]
        public void NewLevelInsertedBeforeProgress_IsNotLocked()
        {
            // Player beat L1..L3, then a new level "NEW" was added between L1 and L2.
            var progress = new LevelProgress();
            progress.RecordWin("L1", 3);
            progress.RecordWin("L2", 3);
            progress.RecordWin("L3", 3);
            var ids = new[] { "L1", "NEW", "L2", "L3", "L4" };

            Assert.That(progress.GetStatus(ids, 1), Is.EqualTo(LevelStatus.Open));
            Assert.That(progress.GetStatus(ids, 4), Is.EqualTo(LevelStatus.Open));
            Assert.That(progress.CurrentIndex(ids), Is.EqualTo(1));
        }

        [Test]
        public void AllCompleted_CurrentIsLastLevel()
        {
            var progress = new LevelProgress();
            foreach (var id in Ids) progress.RecordWin(id, 1);

            Assert.That(progress.CurrentIndex(Ids), Is.EqualTo(Ids.Length - 1));
            Assert.That(progress.TotalStars, Is.EqualTo(4));
        }

        [Test]
        public void SaveData_RoundTrips()
        {
            var original = new LevelProgress();
            original.RecordWin("L1", 3);
            original.RecordWin("L2", 1);

            var loaded = new LevelProgress(original.ToData());

            Assert.That(loaded.GetStars("L1"), Is.EqualTo(3));
            Assert.That(loaded.GetStars("L2"), Is.EqualTo(1));
            Assert.That(loaded.GetStatus(Ids, 2), Is.EqualTo(LevelStatus.Open));
        }

        [Test]
        public void BrokenSaveData_IsTolerated()
        {
            var data = new ProgressData
            {
                levels = new List<LevelRecord>
                {
                    null,
                    new LevelRecord { id = null, stars = 3 },
                    new LevelRecord { id = "", stars = 3 },
                    new LevelRecord { id = "L1", stars = -2 },
                    new LevelRecord { id = "L2", stars = 1 },
                    new LevelRecord { id = "L2", stars = 7 },
                },
            };

            var progress = new LevelProgress(data);

            Assert.That(progress.GetStars("L1"), Is.EqualTo(0));
            Assert.That(progress.GetStars("L2"), Is.EqualTo(3), "duplicate: best kept, clamped to 3");
            Assert.That(new LevelProgress(null).TotalStars, Is.EqualTo(0));
            Assert.That(new LevelProgress(new ProgressData { levels = null }).TotalStars, Is.EqualTo(0));
        }

        [Test]
        public void UnknownLevelIds_AreIgnoredByTheMap()
        {
            // A level that was deleted from the list still has a record; it must not unlock anything.
            var progress = new LevelProgress();
            progress.RecordWin("DELETED", 3);

            Assert.That(progress.GetStatus(Ids, 1), Is.EqualTo(LevelStatus.Locked));
        }
    }
}
