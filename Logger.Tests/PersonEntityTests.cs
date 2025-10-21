using System;
using Xunit;

namespace Logger.Tests;

public class PersonEntityTests
{
    [Fact]
    public void PersonEntity_WithFullName_ReturnsFullName()
    {
        var full = new FullName("Jane", "Doe", "A.");
        var person = new TestPerson(full) { Id = Guid.NewGuid() };
        Assert.Equal(full.Name, person.Name);
    }
    
    [Fact]
    public void PersonEntity_Equals_SameFullName_ReturnsFalse()
    {
        var full = new FullName("Jordan", "Parker");
        var p1 = new TestPerson(full) { Id = Guid.NewGuid() };
        var p2 = new TestPerson(new FullName("Jordan", "Parker")) { Id = Guid.NewGuid() };
        Assert.False(p1.Equals(p2));
    }
    
    [Fact]
    public void PersonEntity_NotEqual_DifferentFullName_ReturnsFalse()
    {
        var p1 = new TestPerson(new FullName("Jordan", "Parker")) { Id = Guid.NewGuid() };
        var p2 = new TestPerson(new FullName("Jordan", "Smith")) { Id = Guid.NewGuid() };
        Assert.False(p1.Equals(p2));
    }

    // Minimal concrete type for testing the abstract PersonEntity
    private sealed record TestPerson(FullName FullName) : PersonEntity(FullName);
}
