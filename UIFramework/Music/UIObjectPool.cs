using System.Collections.Generic;
using UnityEngine;

namespace ChillPatcher.UIFramework.Music
{
    /// <summary>
    /// UI对象池实现
    /// </summary>
    public class UIObjectPool<T> : Core.IUIObjectPool<T> where T : Component
    {
        private readonly Queue<T> _pool;
        private readonly HashSet<T> _activeObjects;
        private readonly GameObject _prefab;
        private readonly Transform _parent;

        public int ActiveCount => _activeObjects.Count;
        public int PooledCount => _pool.Count;

        public UIObjectPool(GameObject prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new Queue<T>();
            _activeObjects = new HashSet<T>();
        }

        public T Get()
        {
            T item;

            // 最终方案：总是创建新对象，不复用
            // 原因：ButtonEventObservable的Subject是readonly，无法清空订阅者
            // 复用会导致事件订阅累积
            var go = UnityEngine.Object.Instantiate(_prefab, _parent);
            item = go.GetComponent<T>();

            if (item == null)
            {
                BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogError($"Prefab does not have component {typeof(T).Name}");
                UnityEngine.Object.Destroy(go);
                return null;
            }

            _activeObjects.Add(item);
            return item;
        }

        public void Return(T item)
        {
            if (item == null)
                return;

            if (!_activeObjects.Contains(item))
            {
                BepInEx.Logging.Logger.CreateLogSource("ChillUIFramework").LogWarning($"Attempting to return object not from this pool");
                return;
            }

            _activeObjects.Remove(item);
            
            // 最终方案：直接销毁，不复用
            // 原因：ButtonEventObservable的Subject是readonly，无法清空订阅者
            // 复用会导致事件订阅累积，必须销毁
            UnityEngine.Object.Destroy(item.gameObject);
        }

        public void Clear()
        {
            // 销毁所有对象
            foreach (var item in _activeObjects)
            {
                if (item != null)
                {
                    UnityEngine.Object.Destroy(item.gameObject);
                }
            }

            while (_pool.Count > 0)
            {
                var item = _pool.Dequeue();
                if (item != null)
                {
                    UnityEngine.Object.Destroy(item.gameObject);
                }
            }

            _activeObjects.Clear();
        }
    }
}

