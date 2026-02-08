using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public class FixedSizeQueue<T>
{
    private readonly Queue<T> _queue = new Queue<T>();
    private readonly Lock _queueLockObj = new();
    public int Capacity { get; internal set; }

    public FixedSizeQueue(int maxSize)
    {
        Capacity = maxSize;
    }

    public void Enqueue(T item)
    {
        lock (_queueLockObj)
        {
            if (_queue.Count >= Capacity)
            {
                _queue.Dequeue();
            }
            _queue.Enqueue(item);
        }
    }

    public T Dequeue()
    {
        lock (_queueLockObj)
        {
            if (_queue.Count == 0)
            {
                throw new InvalidOperationException("The queue is empty.");
            }
            return _queue.Dequeue();
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
}
