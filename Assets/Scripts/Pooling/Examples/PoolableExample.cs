using UnityEngine;

namespace T60.Pooling.Examples
{
    /// <summary>
    /// Example script demonstrating how any component (e.g. Card Visual, Particle FX, Projectile)
    /// can implement IPoolable to automatically receive spawn/despawn lifecycle events.
    /// </summary>
    public class PoolableExample : MonoBehaviour, IPoolable
    {
        [SerializeField] private float autoDespawnDelay = 3.0f;

        public void OnSpawn()
        {
            Debug.Log($"[PoolableExample] '{gameObject.name}' was spawned from pool! Resetting state...", gameObject);
            
            // Automatically despawn itself after autoDespawnDelay seconds if configured
            if (autoDespawnDelay > 0f)
            {
                ObjectPoolManager.DespawnObject(gameObject, autoDespawnDelay);
            }
        }

        public void OnDespawn()
        {
            Debug.Log($"[PoolableExample] '{gameObject.name}' was despawned back to pool!", gameObject);
        }
    }
}
