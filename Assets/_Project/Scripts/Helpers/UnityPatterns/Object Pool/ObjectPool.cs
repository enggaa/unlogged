using System;
using UnityEngine;

namespace Patterns.ObjectPool
{
    class ObjectPool<T> where T : UnityEngine.Object
    {
        private T prefab;
        private bool initialized = false;
        private int size;
        private T[] pool;
        private int iterator = 0;

        public ObjectPool(T prefab, int size)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "ObjectPool size must be greater than zero.");
            }

            this.prefab = prefab;
            this.size = size;
            this.initialized = false;
            Initialize();
        }

        private void Initialize()
        {
            if (!initialized)
            {
                pool = new T[size];
                for (int i = 0; i < size; i++)
                {
                    pool[i] = UnityEngine.Object.Instantiate<T>(prefab);
                }

                iterator = 0;
                initialized = true;
            }
        }

        public T Fetch()
        {
            if (pool == null || pool.Length == 0)
            {
                return null;
            }

            T item = pool[iterator % pool.Length];
            iterator = (iterator + 1) % pool.Length;
            return item;
        }

        public T[] FetchAll()
        {
            return pool;
        }

        public void ExecAll(Action<T> a)
        {
            foreach (var clone in pool)
                a(clone);
        }
    }
}