namespace Models;

public class FixedSizeStack<T>
{
    private readonly Stack<T> _queue = [];
    private readonly Lock _queueLockObj = new();
    public int Capacity { get; internal set; }

    public FixedSizeStack(int maxSize)
    {
        Capacity = maxSize;
    }

    public void Push(T item)
    {
        lock (_queueLockObj)
        {
            if (_queue.Count >= Capacity)
            {
                _queue.Pop();
            }
            _queue.Push(item);
        }
    }

    public T Pop()
    {
        lock (_queueLockObj)
        {
            if (_queue.Count == 0)
            {
                throw new InvalidOperationException("The queue is empty.");
            }
            return _queue.Pop();
        }
    }

    public int Count
    {
        get
        {
            lock (_queueLockObj)
            {
                return _queue.Count;
            }
        }
    }

    public override string? ToString() => string.Join(",", _queue);

    public void Clear()
    {
        _queue.Clear();
    }
}
