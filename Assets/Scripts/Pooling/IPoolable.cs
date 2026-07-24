namespace T60.Pooling
{
    /// <summary>
    /// Optional interface that pooled GameObjects or Components can implement
    /// to receive notifications when spawned from or returned to an ObjectPool.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called immediately after the object is activated and retrieved from the pool.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Called immediately before the object is deactivated and returned to the pool.
        /// </summary>
        void OnDespawn();
    }
}
