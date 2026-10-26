using System.Collections.Generic;
using UnityEngine;

namespace Flowers.View
{
    /// <summary>
    /// Reuses flower objects instead of Instantiate/Destroy on every pop.
    /// Released flowers are just deactivated and handed out again later.
    /// </summary>
    public sealed class FlowerPool
    {
        private readonly FlowerView _prefab;
        private readonly Transform _parent;
        private readonly Stack<FlowerView> _free = new Stack<FlowerView>();
        private readonly List<FlowerView> _all = new List<FlowerView>();

        public FlowerPool(FlowerView prefab, Transform parent, int prewarm)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < prewarm; i++)
                Release(Create());
        }

        /// <summary>Makes sure at least this many flowers exist in total, so play doesn't instantiate.</summary>
        public void Prewarm(int total)
        {
            while (_all.Count < total)
                Release(Create());
        }

        /// <summary>How many flowers are out of the pool right now.</summary>
        public int ActiveCount => _all.Count - _free.Count;

        public FlowerView Get()
        {
            var flower = _free.Count > 0 ? _free.Pop() : Create();
            flower.gameObject.SetActive(true);
            return flower;
        }

        public void Release(FlowerView flower)
        {
            if (!flower.gameObject.activeSelf && _free.Contains(flower)) return; // already back
            flower.SetOnTop(false);
            flower.gameObject.SetActive(false);
            _free.Push(flower);
        }

        /// <summary>Takes back every flower that is out, including ones nobody holds a reference to anymore.</summary>
        public void ReleaseAll()
        {
            for (int i = 0; i < _all.Count; i++)
                if (_all[i].gameObject.activeSelf)
                    Release(_all[i]);
        }

        private FlowerView Create()
        {
            var flower = Object.Instantiate(_prefab, _parent);
            _all.Add(flower);
            return flower;
        }
    }
}
