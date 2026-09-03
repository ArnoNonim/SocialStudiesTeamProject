using UnityEngine;
using UnityEngine.Pool;

namespace _00_Members.PTY.Scripts
{
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    public class PoolManager<T> where T : Component
    {
        private readonly ObjectPool<T> _pool;
        private readonly Transform _root;

        public PoolManager(T prefab, Transform poolFolder, int defaultCapacity = 8, int maxSize = 10, bool prewarm = true)
        {
            _root = poolFolder;

            _pool = new ObjectPool<T>(
                createFunc: () =>
                {
                    var instance = Object.Instantiate(prefab, _root);
                    instance.gameObject.SetActive(false);
                    return instance;
                },
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: instance => Object.Destroy(instance.gameObject),
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            if (prewarm) Prewarm(defaultCapacity);
        }

        private void OnGet(T instance)
        {
            instance.gameObject.SetActive(true);
            if (instance is IPoolable p) p.OnSpawned();
        }

        private void OnRelease(T instance)
        {
            if (instance is IPoolable p) p.OnDespawned();
            instance.gameObject.SetActive(false);
        }

        private void Prewarm(int count)
        {
            var temp = new T[count];
            for (int i = 0; i < count; i++) temp[i] = _pool.Get();
            for (int i = 0; i < count; i++) _pool.Release(temp[i]);
        }

        public T Get() => _pool.Get();
        public void Release(T instance) => _pool.Release(instance);
    }
}