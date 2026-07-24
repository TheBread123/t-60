using UnityEngine;

namespace T60.Pooling.Examples
{
    public class PoolableExample : MonoBehaviour, IPoolable
    {
        [SerializeField] private float autoDespawnDelay = 3.0f;

        public void OnSpawn()
        {
            if (autoDespawnDelay > 0f)
            {
                ObjectPoolManager.DespawnObject(gameObject, autoDespawnDelay);
            }
        }

        public void OnDespawn() { }
    }
}
