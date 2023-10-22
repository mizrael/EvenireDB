namespace EvenireDB.Utils
{
    public class LRUCache<TKey, TValue>
        where TKey : notnull
    {
        private class Node
        {
            public TKey Key { get; init; }
            public TValue Value { get; init; }
            public Node? Next { get; set; }
            public Node? Previous { get; set; }
        }

        private readonly Dictionary<TKey, Node> _cache;
        private readonly uint _capacity;
        private Node? _head;
        private Node? _tail;

        public LRUCache(uint capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, Node>((int)capacity);
        }

        public async ValueTask<TValue> GetOrAddAsync(
            TKey key, 
            Func<TKey, CancellationToken, ValueTask<TValue>> valueFactory,
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                MoveToHead(node);
                return node.Value;
            }

            var value = await valueFactory(key, cancellationToken).ConfigureAwait(false);
            if (_cache.Count == _capacity)
            {
                _cache.Remove(_tail.Key);
                
                _tail = _tail.Previous;
                
                if(_tail != null)
                    _tail.Next = null;
            }

            node = new Node { Key = key, Value = value, Next = _head };
            if (_head != null)
                _head.Previous = node;

            _head = node;
            if (_tail == null)
                _tail = node;

            _cache.Add(key, node);

            return node.Value;
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
    }
}