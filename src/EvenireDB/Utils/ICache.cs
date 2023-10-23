namespace EvenireDB.Utils
{
    public interface ICache<TKey, TValue> where TKey : notnull
    {
        ValueTask<TValue> GetOrAddAsync(TKey key, Func<TKey, CancellationToken, ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default);
        void AddOrUpdate(TKey key, TValue value);
        void DropOldest(uint maxCount);

        public uint Count { get; }
    }
}