using System;   
using Xunit;

namespace Logger.Tests;

public class EntityBaseTests
{
    [Fact]
    public void EntityBase_DerivedProvidesNameAndId_SetsProperties()
    {
        Guid id = new();
        TestDerived derived = new TestDerived { Id = id };

        Assert.Equal(id, derived.Id);
        Assert.Equal("Test Derived", derived.Name);
    }

    private class TestDerived : Logger.EntityBase
    {
        public override string Name => "Test Derived";
    }
}
