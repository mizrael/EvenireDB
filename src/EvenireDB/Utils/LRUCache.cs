namespace EvenireDB.Utils;

using System.Collections.Concurrent;

public class LRUCache<TKey, TValue> : IDisposable, ICache<TKey, TValue> where TKey : notnull
{
    private class Node
    {
        public required TKey Key { get; init; }
        public required TValue Value { get; set; }
        public Node? Next { get; set; }
        public Node? Previous { get; set; }
    }

    private readonly Dictionary<TKey, Node> _cache;
    private readonly uint _capacity;
    private Node? _head;
    private Node? _tail;

    private readonly ReaderWriterLockSlim _rwLock = new(LockRecursionPolicy.NoRecursion);
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> _factorySemaphores = new();

    private bool _disposed;

    public LRUCache(uint capacity)
    {
        _capacity = capacity;
        _cache = new Dictionary<TKey, Node>((int)capacity);
    }

    public void DropOldest(uint maxCount)
    {
        _rwLock.EnterWriteLock();
        try
        {
            if (_cache.Count == 0)
                return;

            uint countToRemove = Math.Min(maxCount, (uint)_cache.Count);

            if (countToRemove == (uint)_cache.Count)
            {
                _head = null;
                _tail = null;
                _cache.Clear();
                foreach (var key in _factorySemaphores.Keys)
                {
                    if (_factorySemaphores.TryRemove(key, out var sem))
                        sem.Dispose();
                }
                return;
            }

            var curr = _tail;
            while (countToRemove > 0 && curr != null)
            {
                _cache.Remove(curr.Key);
                if (_factorySemaphores.TryRemove(curr.Key, out var evictedSem))
                    evictedSem.Dispose();
                curr = curr.Previous;
                countToRemove--;
            }

            _tail = curr;
            if (_tail is not null)
                _tail.Next = null;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    public void Remove(TKey key)
    {
        _rwLock.EnterWriteLock();
        try
        {
            if (!_cache.TryGetValue(key, out var node))
                return;

            if (_head == node)
                _head = node.Next;

            if (_tail == node)
                _tail = node.Previous;

            if (node.Previous != null)
                node.Previous.Next = node.Next;

            if (node.Next != null)
                node.Next.Previous = node.Previous;

            _cache.Remove(key);
            if (_factorySemaphores.TryRemove(key, out var removedSem))
                removedSem.Dispose();
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    public bool ContainsKey(TKey key)
    {
        _rwLock.EnterReadLock();
        try
        {
            return _cache.ContainsKey(key);
        }
        finally
        {
            _rwLock.ExitReadLock();
        }
    }

    public void AddOrUpdate(TKey key, TValue value)
    {
        _rwLock.EnterWriteLock();
        try
        {
            if (_cache.TryGetValue(key, out var node))
            {
                node.Value = value;
                MoveToHeadUnsafe(node);
            }
            else
            {
                AddUnsafe(key, value);
            }
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    public async ValueTask<TValue> GetOrAddAsync(
        TKey key,
        Func<TKey, CancellationToken, ValueTask<TValue>> valueFactory,
        CancellationToken cancellationToken = default)
    {
        // Fast path: check cache under read lock
        _rwLock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var existingNode))
            {
                // Upgrade to write lock for MoveToHead — release read first
                _rwLock.ExitReadLock();
                _rwLock.EnterWriteLock();
                try
                {
                    // Re-check: node could have been evicted between locks
                    if (_cache.TryGetValue(key, out existingNode))
                    {
                        MoveToHeadUnsafe(existingNode);
                        return existingNode.Value;
                    }
                    // Fall through to factory path
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
                // Read lock was already exited above; fall through to factory path
            }
            else
            {
                _rwLock.ExitReadLock();
            }
        }
        catch
        {
            if (_rwLock.IsReadLockHeld)
                _rwLock.ExitReadLock();
            throw;
        }

        // Slow path: cache miss — use per-key semaphore to deduplicate factory calls
        var semaphore = _factorySemaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check under read lock after acquiring semaphore
            _rwLock.EnterReadLock();
            try
            {
                if (_cache.TryGetValue(key, out var node))
                    return node.Value;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }

            // Factory call (outside any lock)
            var value = await valueFactory(key, cancellationToken).ConfigureAwait(false);

            // Add under write lock
            _rwLock.EnterWriteLock();
            try
            {
                // Final check: another thread could have added between factory and write lock
                if (_cache.TryGetValue(key, out var existingNode))
                {
                    existingNode.Value = value;
                    MoveToHeadUnsafe(existingNode);
                    return existingNode.Value;
                }

                var newNode = AddUnsafe(key, value);
                return newNode.Value;
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Adds a node to head. Caller MUST hold write lock.
    /// </summary>
    private Node AddUnsafe(TKey key, TValue value)
    {
        if (_cache.Count == _capacity && _tail != null)
        {
            _cache.Remove(_tail.Key);
            _tail = _tail.Previous;
            if (_tail != null)
                _tail.Next = null;
        }

        var node = new Node { Key = key, Value = value, Next = _head };
        if (_head != null)
            _head.Previous = node;

        _head = node;
        _tail ??= node;

        _cache.Add(key, node);
        return node;
    }

    /// <summary>
    /// Moves node to head of linked list. Caller MUST hold write lock.
    /// </summary>
    private void MoveToHeadUnsafe(Node node)
    {
        if (node == _head)
            return;

        if (node.Previous != null)
            node.Previous.Next = node.Next;

        if (node.Next != null)
            node.Next.Previous = node.Previous;

        if (_tail == node)
            _tail = node.Previous;

        node.Previous = null;
        node.Next = _head;

        if (_head != null)
            _head.Previous = node;

        _head = node;
    }

    public uint Count
    {
        get
        {
            _rwLock.EnterReadLock();
            try
            {
                return (uint)_cache.Count;
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }
    }

    public IEnumerable<TKey> Keys
    {
        get
        {
            _rwLock.EnterReadLock();
            try
            {
                return _cache.Keys.ToArray();
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _rwLock.EnterWriteLock();
                try
                {
                    foreach (var semaphore in _factorySemaphores.Values)
                        semaphore.Dispose();
                    _factorySemaphores.Clear();
                }
                finally
                {
                    _rwLock.ExitWriteLock();
                }
                _rwLock.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
