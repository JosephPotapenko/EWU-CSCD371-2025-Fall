using System;
using Xunit;

namespace Logger.Tests;

public class EntityRecordsTests
{
    [Fact]
    public void Book_Name_IsTitle()
    {
        var book = new Book("The Hobbit", "Tolkien") { Id = Guid.NewGuid() };
        Assert.Equal("The Hobbit", book.Name);
    }

    [Fact]
    public void Book_Equals_SameTitleAndAuthor_AreNotEqual()
    {
        var b1 = new Book("1984", "Orwell") { Id = Guid.NewGuid() };
        var b2 = new Book("1984", "Orwell") { Id = Guid.NewGuid() };
        Assert.False(b1.Equals(b2));
    }

    [Fact]
    public void Student_Name_IsFullName()
    {
        var full = new FullName("Sam", "Winchester", "J.");
        var student = new Student(full, "S123") { Id = Guid.NewGuid() };
        Assert.Equal(full.Name, student.Name);
    }

    [Fact]
    public void Student_SameFullNameAndStudentId_ReturnsFalse()
    {
        var full = new FullName("Alex", "Mercer");
        var s1 = new Student(full, "S100") { Id = Guid.NewGuid() };
        var s2 = new Student(new FullName("Alex", "Mercer"), "S100") { Id = Guid.NewGuid() };
        Assert.False(s1.Equals(s2));
    }

    [Fact]
    public void Student_DifferentStudentId_ReturnsFalse()
    {
        var full = new FullName("Alex", "Mercer");
        var s1 = new Student(full, "S100") { Id = Guid.NewGuid() };
        var s2 = new Student(full, "S101") { Id = Guid.NewGuid() };
        Assert.False(s1.Equals(s2));
    }

    [Fact]
    public void Employee_Name_IsFullName()
    {
        var full = new FullName("Dean", "Winchester");
        var emp = new Employee(full, "E456") { Id = Guid.NewGuid() };
        Assert.Equal(full.Name, emp.Name);
    }

    [Fact]
    public void Employee_SameFullNameAndEmployeeNumber_ReturnsFalse()
    {
        var full = new FullName("Casey", "Jones");
        var e1 = new Employee(full, "E200") { Id = Guid.NewGuid() };
        var e2 = new Employee(new FullName("Casey", "Jones"), "E200") { Id = Guid.NewGuid() };
        Assert.False(e1.Equals(e2));
    }

    [Fact]
    public void Employee_DifferentEmployeeNumber_ReturnsFalse()
    {
        var full = new FullName("Casey", "Jones");
        var e1 = new Employee(full, "E200") { Id = Guid.NewGuid() };
        var e2 = new Employee(full, "E201") { Id = Guid.NewGuid() };
        Assert.False(e1.Equals(e2));
    }

    [Fact]
    public void StudentAndEmployee_SameFullName_ReturnsFalse()
    {
        var full = new FullName("Robin", "Hood");
        var student = new Student(full, "S1") { Id = Guid.NewGuid() };
        var emp = new Employee(full, "E1") { Id = Guid.NewGuid() };
        Assert.False(student.Equals(emp));
    }
}
