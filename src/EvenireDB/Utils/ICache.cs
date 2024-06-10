namespace EvenireDB.Utils;

public interface ICache<TKey, TValue> where TKey : notnull
{
    bool ContainsKey(TKey key);
    ValueTask<TValue> GetOrAddAsync(TKey key, Func<TKey, CancellationToken, ValueTask<TValue>> valueFactory, CancellationToken cancellationToken = default);
    void AddOrUpdate(TKey key, TValue value);
    void DropOldest(uint maxCount);
    void Remove(TKey key);

    public uint Count { get; }
    IEnumerable<TKey> Keys { get; }
}