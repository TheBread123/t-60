using UnityEngine;

namespace T60.Pooling
{
    [DisallowMultipleComponent]
    public class PooledObjectTracker : MonoBehaviour
    {
        public Pool OriginPool { get; internal set; }

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
