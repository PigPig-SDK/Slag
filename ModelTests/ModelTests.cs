using Models;

namespace ModelTests;

public class ModelTests
{
    [Fact]
    public void DifferentModels_Unequal_Success()
    {
        Model a = new Model();
        Model b = new Model();
        Assert.NotEqual(a,b);
    }
    [Fact]
    public void Null_Nondisposed_Success()
    {
        Model a = new Model();
        Model b = a;
        Assert.Equal(a, b);
    }
    [Fact]
    public void NotDisposed_Null_NotEqual()
    {
        Model? a = new Model();
        Assert.True(a != null);
    }
    [Fact]
    public void Disposed_Null_NotEqual()
    {
        Model? a = new Model();
        a.Dispose();
        Assert.False(a == null);
        Assert.False(null == a);
    }
}
