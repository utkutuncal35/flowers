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

        public FlowerPool(FlowerView prefab, Transform parent, int prewarm)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < prewarm; i++)
                Release(Create());
        }

        public FlowerView Get()
        {
            var flower = _free.Count > 0 ? _free.Pop() : Create();
            flower.gameObject.SetActive(true);
            return flower;
        }

        public void Release(FlowerView flower)
        {
            flower.SetOnTop(false);
            flower.gameObject.SetActive(false);
            _free.Push(flower);
        }

        private FlowerView Create() => Object.Instantiate(_prefab, _parent);
    }
}
