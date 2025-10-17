using System;
using Xunit;

namespace Logger.Tests;

public class IEntityTests
{
    [Fact]
    public void IEntity_Definition_HasIdAndName()
    {
        // The test will compile only if IEntity exists with the required members.
        var id = Guid.NewGuid();
        IEntity impl = new TestEntity { Id = id, Name = "X" };

        Assert.Equal(id, impl.Id);
        Assert.Equal("X", impl.Name);
    }

    private record TestEntity : IEntity
    {
        public Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
