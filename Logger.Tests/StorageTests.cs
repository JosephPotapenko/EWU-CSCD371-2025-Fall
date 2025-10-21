using System;
using Xunit;

namespace Logger.Tests;

public class StorageTests
{
    [Fact]
    public void Storage_AddContainsRemove_Works()
    {
        var storage = new Storage();
        var book = new Book("1984") { Id = Guid.NewGuid() };

        Assert.False(storage.Contains(book));
        storage.Add(book);
        Assert.True(storage.Contains(book));
        storage.Remove(book);
        Assert.False(storage.Contains(book));
    }

    [Fact]
    public void Storage_GetById_ReturnsEntity()
    {
        var storage = new Storage();
        var student = new Student(new FullName("A", "B"), "S1") { Id = Guid.NewGuid() };
        var emp = new Employee(new FullName("C", "D"), "E1") { Id = Guid.NewGuid() };

        storage.Add(student);
        storage.Add(emp);

        Assert.Equal(student, storage.Get(student.Id));
        Assert.Equal(emp, storage.Get(emp.Id));
        Assert.Null(storage.Get(Guid.NewGuid()));
    }
}
