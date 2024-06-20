namespace EvenireDB.Utils;

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

    private object _moveToHeadLock = new();
    private object _dropLock = new();
    private object _getSemaphoresLock = new();

    private readonly Dictionary<TKey, SemaphoreSlim> _semaphores;

    private bool _disposed;

    public LRUCache(uint capacity)
    {
        _capacity = capacity;
        _cache = new Dictionary<TKey, Node>((int)capacity);
        _semaphores = new Dictionary<TKey, SemaphoreSlim>((int)capacity);
    }

    public void DropOldest(uint maxCount)
    {
        if (this.Count == 0)
            return;

        uint countToRemove = Math.Min(maxCount, this.Count);

        lock (_dropLock)
        {
            if (countToRemove == this.Count)
            {
                _head = null;
                _tail = null;
                _cache.Clear();
                _semaphores.Clear();
                return;
            }

            var curr = _tail;
            while (countToRemove > 0 && curr != null)
            {
                _cache.Remove(curr.Key);
                _semaphores.Remove(curr.Key);

                curr = curr.Previous;
                countToRemove--;
            }

            if (curr is not null)
                curr.Next = null;
        }
    }

    public void Remove(TKey key)
    {
        if (!_cache.TryGetValue(key, out var node))
            return;

        SemaphoreSlim semaphore = GetSemaphore(key);
        semaphore.Wait();

        try
        {
            if (_head == node)
                _head = node.Next;

            if (_tail == node)
                _tail = node.Previous;

            if (node.Previous != null)
                node.Previous.Next = node.Next;

            if (node.Next != null)
                node.Next.Previous = node.Previous;

            _cache.Remove(key);
            _semaphores.Remove(key);
        }
        finally
        {
            semaphore.Release();
        }
    }

    public bool ContainsKey(TKey key)
    => _cache.ContainsKey(key);

    public void AddOrUpdate(TKey key, TValue value)
    {
        SemaphoreSlim semaphore = GetSemaphore(key);
        semaphore.Wait();

        if (_cache.TryGetValue(key, out var node))
        {
            node.Value = value;
            MoveToHead(node);
        }
        else
        {
            Add(key, value);
        }

        semaphore.Release();
    }

    public async ValueTask<TValue> GetOrAddAsync(
        TKey key,
        Func<TKey, CancellationToken, ValueTask<TValue>> valueFactory,
        CancellationToken cancellationToken = default)
    {
        if (!_cache.TryGetValue(key, out var node))
        {
            SemaphoreSlim semaphore = GetSemaphore(key);
            semaphore.Wait(cancellationToken);

            if (!_cache.TryGetValue(key, out node))
                node = await AddAsync(key, valueFactory, cancellationToken).ConfigureAwait(false);

            semaphore.Release();
        }
        else
        {
            MoveToHead(node);
        }

        return node.Value;
    }

    private async ValueTask<Node> AddAsync(
        TKey key,
        Func<TKey, CancellationToken, ValueTask<TValue>> valueFactory,
        CancellationToken cancellationToken)
    {
        var value = await valueFactory(key, cancellationToken).ConfigureAwait(false);
        return Add(key, value);
    }

    private Node Add(TKey key, TValue value)
    {
        if (_cache.Count == _capacity)
        {
            _cache.Remove(_tail.Key);
            _semaphores.Remove(_tail.Key);

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

    private SemaphoreSlim GetSemaphore(TKey key)
    {
        if (_semaphores.TryGetValue(key, out var semaphore))
            return semaphore;

        lock (_getSemaphoresLock)
        {
            if (!_semaphores.TryGetValue(key, out semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                _semaphores.Add(key, semaphore);
            }

            return semaphore;
        }
    }

    private void MoveToHead(Node node)
    {
        lock (_moveToHeadLock)
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
    }

    public uint Count => (uint)_cache.Count;

    public IEnumerable<TKey> Keys => _cache.Keys;

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                foreach (var semaphore in _semaphores.Values)
                    semaphore.Dispose();
                _semaphores.Clear();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
