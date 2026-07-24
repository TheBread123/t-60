using System.Collections.Generic;
using UnityEngine;

namespace T60.Pooling
{
    public class Pool
    {
        public GameObject Prefab { get; private set; }
        public Transform RootParent { get; private set; }
        public int TotalCreatedCount => _inactiveQueue.Count + _activeSet.Count;
        public int ActiveCount => _activeSet.Count;
        public int InactiveCount => _inactiveQueue.Count;

        private readonly Queue<GameObject> _inactiveQueue = new Queue<GameObject>();
        private readonly HashSet<GameObject> _activeSet = new HashSet<GameObject>();

        public Pool(GameObject prefab, Transform rootParent, int initialPrewarmCount = 0)
        {
            Prefab = prefab;
            RootParent = rootParent;

            if (initialPrewarmCount > 0)
            {
                Prewarm(initialPrewarmCount);
            }
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject instance = CreateNewInstance();
                _inactiveQueue.Enqueue(instance);
            }
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject instance = _inactiveQueue.Count > 0 ? _inactiveQueue.Dequeue() : CreateNewInstance();

            if (instance == null)
            {
                Debug.LogError($"[Pool] Failed to retrieve valid instance for prefab '{Prefab.name}'.");
                return null;
            }

            _activeSet.Add(instance);

            Transform t = instance.transform;
            t.SetParent(parent != null ? parent : RootParent, false);
            t.SetPositionAndRotation(position, rotation);

            instance.SetActive(true);

            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawn();
            }

            return instance;
        }

        public bool Despawn(GameObject instance)
        {
            if (instance == null) return false;

            if (!_activeSet.Contains(instance))
            {
                if (_inactiveQueue.Contains(instance)) return false;
            }
            else
            {
                _activeSet.Remove(instance);
            }

            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnDespawn();
            }

            instance.SetActive(false);
            if (RootParent != null)
            {
                instance.transform.SetParent(RootParent, false);
            }

            _inactiveQueue.Enqueue(instance);
            return true;
        }

        public void Clear()
        {
            foreach (GameObject obj in _activeSet)
            {
                if (obj != null) Object.Destroy(obj);
            }
            _activeSet.Clear();

            while (_inactiveQueue.Count > 0)
            {
                GameObject obj = _inactiveQueue.Dequeue();
                if (obj != null) Object.Destroy(obj);
            }
        }

        private GameObject CreateNewInstance()
        {
            GameObject instance = Object.Instantiate(Prefab, RootParent);
            instance.name = $"{Prefab.name}_PooledInstance";

            if (!instance.TryGetComponent(out PooledObjectTracker tracker))
            {
                tracker = instance.AddComponent<PooledObjectTracker>();
            }
            tracker.OriginPool = this;

            instance.SetActive(false);
            return instance;
        }
    }
}
