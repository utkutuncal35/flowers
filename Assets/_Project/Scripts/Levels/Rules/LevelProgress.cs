using System;
using System.Collections.Generic;

namespace Flowers.Levels
{
    /// <summary>What gets saved to the phone. Plain fields so Unity's JsonUtility can write it.</summary>
    [Serializable]
    public class ProgressData
    {
        public int version = 1;
        public List<LevelRecord> levels = new List<LevelRecord>();
    }

    [Serializable]
    public class LevelRecord
    {
        /// <summary>The LevelData asset name. Renaming a level asset resets its saved stars.</summary>
        public string id;
        public int stars;
    }

    public enum LevelStatus
    {
        Locked,
        /// <summary>Unlocked but not won yet.</summary>
        Open,
        Completed,
    }

    /// <summary>
    /// Saved stars per level and which levels are unlocked.
    /// A level is unlocked if it is the first one, or every level up to the one before it has been reached:
    /// precisely, index &lt;= (highest completed index) + 1. That way inserting a new level in the middle
    /// later doesn't lock the player out of levels they already beat.
    /// </summary>
    public sealed class LevelProgress
    {
        public const int MaxStars = 3;

        private readonly Dictionary<string, int> _stars = new Dictionary<string, int>();

        public LevelProgress(ProgressData data = null)
        {
            if (data?.levels == null) return;
            foreach (var record in data.levels)
            {
                // Tolerate a hand-edited or partly broken save: skip junk, keep the best duplicate.
                if (record == null || string.IsNullOrEmpty(record.id) || record.stars <= 0) continue;
                int stars = Math.Min(record.stars, MaxStars);
                if (!_stars.TryGetValue(record.id, out int existing) || stars > existing)
                    _stars[record.id] = stars;
            }
        }

        public int GetStars(string levelId) =>
            levelId != null && _stars.TryGetValue(levelId, out int stars) ? stars : 0;

        public bool IsCompleted(string levelId) => GetStars(levelId) > 0;

        public int TotalStars
        {
            get
            {
                int total = 0;
                foreach (var stars in _stars.Values) total += stars;
                return total;
            }
        }

        public LevelStatus GetStatus(IReadOnlyList<string> orderedIds, int index)
        {
            if (IsCompleted(orderedIds[index])) return LevelStatus.Completed;
            return index <= HighestCompletedIndex(orderedIds) + 1 ? LevelStatus.Open : LevelStatus.Locked;
        }

        public bool IsUnlocked(IReadOnlyList<string> orderedIds, int index) =>
            GetStatus(orderedIds, index) != LevelStatus.Locked;

        /// <summary>The level the map should point at: the first open one, or the last level if all are done.</summary>
        public int CurrentIndex(IReadOnlyList<string> orderedIds)
        {
            for (int i = 0; i < orderedIds.Count; i++)
                if (GetStatus(orderedIds, i) == LevelStatus.Open)
                    return i;
            return Math.Max(0, orderedIds.Count - 1);
        }

        /// <summary>Stores a win. Keeps the best result. Returns true if this is a new best.</summary>
        public bool RecordWin(string levelId, int stars)
        {
            if (string.IsNullOrEmpty(levelId)) throw new ArgumentException("Level id is empty.", nameof(levelId));
            stars = Math.Max(1, Math.Min(stars, MaxStars));
            if (stars <= GetStars(levelId)) return false;
            _stars[levelId] = stars;
            return true;
        }

        public ProgressData ToData()
        {
            var data = new ProgressData();
            foreach (var pair in _stars)
                data.levels.Add(new LevelRecord { id = pair.Key, stars = pair.Value });
            return data;
        }

        private int HighestCompletedIndex(IReadOnlyList<string> orderedIds)
        {
            for (int i = orderedIds.Count - 1; i >= 0; i--)
                if (IsCompleted(orderedIds[i]))
                    return i;
            return -1;
        }
    }
}
