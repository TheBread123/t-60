using System.Collections.Generic;
using UnityEngine;

namespace T60.Pooling
{
    /// <summary>
    /// Manages an isolated object pool for a specific GameObject prefab.
    /// </summary>
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

        /// <summary>
        /// Instantiates multiple instances in advance and stores them inactive in the pool.
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject instance = CreateNewInstance();
                _inactiveQueue.Enqueue(instance);
            }
        }

        /// <summary>
        /// Retrieves an instance from the pool (or instantiates a new one if empty),
        /// activates it, and notifies IPoolable listeners.
        /// </summary>
        public GameObject Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject instance;

            if (_inactiveQueue.Count > 0)
            {
                instance = _inactiveQueue.Dequeue();
            }
            else
            {
                instance = CreateNewInstance();
            }

            if (instance == null)
            {
                Debug.LogError($"[Pool] Failed to retrieve valid instance for prefab '{Prefab.name}'.");
                return null;
            }

            _activeSet.Add(instance);

            // Configure Transform
            Transform t = instance.transform;
            t.SetParent(parent != null ? parent : RootParent, false);
            t.SetPositionAndRotation(position, rotation);

            // Activate GameObject
            instance.SetActive(true);

            // Notify IPoolable components
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnSpawn();
            }

            return instance;
        }

        /// <summary>
        /// Returns an active instance back to the pool, deactivates it, and notifies IPoolable listeners.
        /// </summary>
        public bool Despawn(GameObject instance)
        {
            if (instance == null) return false;

            if (!_activeSet.Contains(instance))
            {
                // Safety check: avoid double despawning
                if (_inactiveQueue.Contains(instance))
                {
                    Debug.LogWarning($"[Pool] Attempted to despawn object '{instance.name}' which is already in the inactive pool!", instance);
                    return false;
                }

                Debug.LogWarning($"[Pool] Object '{instance.name}' was not recognized as active in pool '{Prefab.name}'. Forcing despawn.", instance);
            }
            else
            {
                _activeSet.Remove(instance);
            }

            // Notify IPoolable components before deactivation
            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i].OnDespawn();
            }

            // Deactivate and reset hierarchy
            instance.SetActive(false);
            if (RootParent != null)
            {
                instance.transform.SetParent(RootParent, false);
            }

            _inactiveQueue.Enqueue(instance);
            return true;
        }

        /// <summary>
        /// Clears and destroys all instances managed by this pool.
        /// </summary>
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

            // Attach tracker component for rapid pool identification and self-despawning
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
