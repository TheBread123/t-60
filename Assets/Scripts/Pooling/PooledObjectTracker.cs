using UnityEngine;

namespace T60.Pooling
{
    /// <summary>
    /// Component attached to pooled instances to track their source pool
    /// and allow convenient self-despawning.
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledObjectTracker : MonoBehaviour
    {
        public Pool OriginPool { get; internal set; }

        /// <summary>
        /// Convenient method to return this object to its origin pool.
        /// </summary>
        public void Despawn()
        {
            if (OriginPool != null)
            {
                OriginPool.Despawn(gameObject);
            }
            else
            {
                Debug.LogWarning($"[PooledObjectTracker] Object '{name}' has no origin pool! Destroying object instead.", gameObject);
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Convenient method to return this object to its origin pool after a delay.
        /// </summary>
        public void Despawn(float delaySeconds)
        {
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.Despawn(gameObject, delaySeconds);
            }
            else
            {
                Destroy(gameObject, delaySeconds);
            }
        }
    }
}
