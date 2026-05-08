namespace Core;

/// <summary>
/// A list where when the capacity is reached, the oldest item is discarded.
/// </summary>
public class FixedSizeList<T>
{
    private readonly LinkedList<T> _list = new();
    private readonly Lock _lockObj = new();

    public int Capacity { get; internal set; }

    public FixedSizeList(int maxSize)
    {
        Capacity = maxSize;
    }

    public void Push(T item)
    {
        lock (_lockObj)
        {
            if (_list.Count >= Capacity)
            {
                _list.RemoveLast(); // Remove oldest (bottom of stack)
            }
            _list.AddFirst(item); // Push to top
        }
    }

    public T Pop()
    {
        lock (_lockObj)
        {
            if (_list.Count == 0)
                throw new InvalidOperationException("The stack is empty.");

            T value = _list.First!.Value;
            _list.RemoveFirst();
            return value;
        }
    }

    public T Peek()
    {
        lock (_lockObj)
        {
            if (_list.Count == 0)
                throw new InvalidOperationException("The stack is empty.");

            return _list.First!.Value;
        }
    }

    public int Count
    {
        get
        {
            lock (_lockObj)
            {
                return _list.Count;
            }
        }
    }

    public void Clear()
    {
        lock (_lockObj)
        {
            _list.Clear();
        }
    }

    public override string? ToString()
    {
        lock (_lockObj)
        {
            return string.Join(",", _list);
        }
    }
}