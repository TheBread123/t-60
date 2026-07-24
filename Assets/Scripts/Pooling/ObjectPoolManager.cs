using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace T60.Pooling
{
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        [System.Serializable]
        public struct PrewarmConfig
        {
            public GameObject prefab;
            public int initialCount;
        }

        [Header("Pool Prewarming Setup")]
        [SerializeField] private List<PrewarmConfig> prewarmConfigs = new List<PrewarmConfig>();
        [SerializeField] private bool dontDestroyOnLoad = true;

        private readonly Dictionary<GameObject, Pool> _poolsByPrefab = new Dictionary<GameObject, Pool>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            InitializePrewarmPools();
        }

        private void InitializePrewarmPools()
        {
            foreach (var config in prewarmConfigs)
            {
                if (config.prefab != null && config.initialCount > 0)
                {
                    GetOrCreatePool(config.prefab, config.initialCount);
                }
            }
        }

        public Pool GetOrCreatePool(GameObject prefab, int prewarmCount = 0)
        {
            if (prefab == null) return null;

            if (!_poolsByPrefab.TryGetValue(prefab, out Pool pool))
            {
                GameObject poolRoot = new GameObject($"Pool_{prefab.name}");
                poolRoot.transform.SetParent(transform, false);

                pool = new Pool(prefab, poolRoot.transform, prewarmCount);
                _poolsByPrefab.Add(prefab, pool);
            }
            else if (prewarmCount > 0)
            {
                pool.Prewarm(prewarmCount);
            }

            return pool;
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            Pool pool = GetOrCreatePool(prefab);
            return pool?.Spawn(position, rotation, parent);
        }

        public T Spawn<T>(T prefabComponent, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            if (prefabComponent == null) return null;

            GameObject spawnedObj = Spawn(prefabComponent.gameObject, position, rotation, parent);
            return spawnedObj != null ? spawnedObj.GetComponent<T>() : null;
        }

        public bool Despawn(GameObject instance)
        {
            if (instance == null) return false;

            if (instance.TryGetComponent(out PooledObjectTracker tracker) && tracker.OriginPool != null)
            {
                return tracker.OriginPool.Despawn(instance);
            }

            Destroy(instance);
            return false;
        }

        public void Despawn(GameObject instance, float delaySeconds)
        {
            if (delaySeconds <= 0f)
            {
                Despawn(instance);
            }
            else
            {
                StartCoroutine(DespawnRoutine(instance, delaySeconds));
            }
        }

        private IEnumerator DespawnRoutine(GameObject instance, float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            Despawn(instance);
        }

        public static GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            EnsureInstanceExists();
            return Instance.Spawn(prefab, position, rotation, parent);
        }

        public static T SpawnObject<T>(T prefabComponent, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
        {
            EnsureInstanceExists();
            return Instance.Spawn(prefabComponent, position, rotation, parent);
        }

        public static void DespawnObject(GameObject instance)
        {
            if (Instance != null)
            {
                Instance.Despawn(instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        public static void DespawnObject(GameObject instance, float delaySeconds)
        {
            if (Instance != null)
            {
                Instance.Despawn(instance, delaySeconds);
            }
            else
            {
                Destroy(instance, delaySeconds);
            }
        }

        private static void EnsureInstanceExists()
        {
            if (Instance == null)
            {
                GameObject managerObj = new GameObject("[ObjectPoolManager]");
                Instance = managerObj.AddComponent<ObjectPoolManager>();
            }
        }
    }
}
