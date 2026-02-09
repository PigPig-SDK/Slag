using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace ModelTests;

public class FixedSizeQueueTests
{
    [Fact]
    public void SizeOne_EnqueueDequeue_Accepted()
    {
        FixedSizeStack<int> queue = new(1);
        queue.Push(1);
        queue.Push(2);
        int output = queue.Pop();
        Assert.Equal(2, output);
    }
    [Fact]
    public void SizeTwo_EnqueueDequeue_Accepted()
    {
        FixedSizeStack<int> queue = new(2);
        queue.Push(1);
        queue.Push(2);
        int output = queue.Pop();
        int output2 = queue.Pop();
        Assert.Equal(2, output);
        Assert.Equal(1, output2);
    }
    [Fact]
    public void Dequeue_Enqueue_Accepted()
    {
        FixedSizeStack<int> queue = new(1);
        Assert.Throws<InvalidOperationException>(() => queue.Pop());
    }
}
