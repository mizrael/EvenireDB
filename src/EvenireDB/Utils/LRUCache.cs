namespace EvenireDB.Utils
{
    // TODO: drop entries if memory consumption is approaching a threshold
    // TODO: add item expiration
    public class LRUCache<TKey, TValue> : IDisposable, ICache<TKey, TValue> where TKey : notnull
    {
        private class Node
        {
            public TKey Key { get; init; }
            public TValue Value { get; set; }
            public Node? Next { get; set; }
            public Node? Previous { get; set; }
        }

        private readonly Dictionary<TKey, Node> _cache;
        private readonly uint _capacity;
        private Node? _head;
        private Node? _tail;

        private object _lock = new();
        private readonly Dictionary<TKey, SemaphoreSlim> _semaphores;

        private bool _disposed;

        public LRUCache(uint capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, Node>((int)capacity);
            _semaphores = new Dictionary<TKey, SemaphoreSlim>((int)capacity);
        }

        public void Update(TKey key, TValue value)
        {
            if (!_cache.ContainsKey(key))
                throw new KeyNotFoundException($"invalid key: {key}");

            SemaphoreSlim semaphore = GetSemaphore(key);
            semaphore.Wait();

            _cache[key].Value = value;

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

        private async Task<LRUCache<TKey, TValue>.Node?> AddAsync(
            TKey key,
            Func<TKey, CancellationToken, ValueTask<TValue>> valueFactory,
            CancellationToken cancellationToken)
        {
            var value = await valueFactory(key, cancellationToken).ConfigureAwait(false);
            if (_cache.Count == _capacity)
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

        private SemaphoreSlim GetSemaphore(TKey key)
        {
            if (_semaphores.TryGetValue(key, out var semaphore))
                return semaphore;

            lock (_lock)
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

        public int Count => _cache.Count;

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
}