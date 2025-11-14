using Assignment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Assignment.Tests;

[TestClass]
public class NodeTests
{
    [TestMethod]
    public void NewNode_HasSelfLoopInNext()
    {
        var n = new Node<int>(42);
        Assert.IsNotNull(n.Next);
        Assert.AreSame(n, n.Next);
    }

    [TestMethod]
    public void ToString_DelegatesToValueToString_ForValueTypes()
    {
        var n = new Node<int>(123);
        Assert.AreEqual<string>("123", n.ToString());
    }

    [TestMethod]
    public void ToString_HandlesNullableReferenceValue()
    {
        var n = new Node<string?>(null);
        Assert.AreEqual<string>(string.Empty, n.ToString());
    }

    [TestMethod]
    public void Next_HasPrivateSetter()
    {
        var n = new Node<int>(1);
        Assert.AreSame(n, n.Next);
    }

    [TestMethod]
    public void Append_AddsNewNodeAfterCurrentNode_Success()
    {
        var n1 = new Node<string>("first");
        n1.Append("second");
        Assert.AreEqual("second", n1.Next.ToString());
        Assert.AreSame(n1, n1.Next.Next);
    }

    [TestMethod]
    public void Append_DuplicateOnHead_ThrowsInvalidOperationException()
    {
        // Arrange
        Node<int> n1 = new Node<int>(10);

        // Act
        void Act() { n1.Append(10); }

        // Assert
        Assert.ThrowsExactly<InvalidOperationException>(Act);
    }

    [TestMethod]
    public void Append_DuplicateInMiddle_ThrowsInvalidOperationException()
    {
        // Arrange
        Node<int> n1 = new Node<int>(10);
        n1.Append(20);
        n1.Append(30);

        // Act
        void Act() { n1.Append(20); }

        // Assert
        Assert.ThrowsExactly<InvalidOperationException>(Act);
    }

    [TestMethod]
    public void Append_DuplicateNullReference_ThrowsInvalidOperationException()
    {
        // Arrange
        Node<string?> n1 = new Node<string?>(null);
        n1.Append("x");

        // Act
        void Act() { n1.Append(null); }

        // Assert
        Assert.ThrowsExactly<InvalidOperationException>(Act);
    }

    [TestMethod]
    public void Clear_RemovesAllButCurrentNode_Success()
    {
        // arrange
        Node<int> n1 = new Node<int>(1);
        n1.Append(2);
        n1.Append(3);
        // act
        n1.Clear();
        // assert
        Assert.AreSame(n1, n1.Next);
    }

    [TestMethod]
    public void Clear_OnSingleNodeList_DoesNothing()
    {
        // arrange
        Node<int> n1 = new Node<int>(1);
        // act
        n1.Clear();
        // assert
        Assert.AreSame(n1, n1.Next);
    }

    [TestMethod]
    public void Exists_FindsHeadValue_ReturnsTrue()
    {
        // Arrange
        Node<int> n1 = new Node<int>(10);
        n1.Append(20);
        n1.Append(30);

        // Act
        bool found = n1.Exists(10);

        // Assert
        Assert.IsTrue(found);
    }

    [TestMethod]
    public void Exists_FindsMiddleValue_ReturnsTrue()
    {
        // Arrange
        Node<int> n1 = new Node<int>(10);
        n1.Append(20);
        n1.Append(30);

        // Act
        bool found = n1.Exists(20);

        // Assert
        Assert.IsTrue(found);
    }

    [TestMethod]
    public void Exists_FindsTailValue_ReturnsTrue()
    {
        // Arrange
        Node<int> n1 = new Node<int>(10);
        n1.Append(20);
        n1.Append(30);

        // Act
        bool found = n1.Exists(30);

        // Assert
        Assert.IsTrue(found);
    }

    [TestMethod]
    public void Exists_MissingValue_ReturnsFalse()
    {
        // Arrange
        Node<int> n1 = new Node<int>(10);
        n1.Append(20);
        n1.Append(30);

        // Act
        bool found = n1.Exists(40);

        // Assert
        Assert.IsFalse(found);
    }

    [TestMethod]
    public void Exists_SingleNodePresent_ReturnsTrue()
    {
        // Arrange
        Node<int> n = new Node<int>(7);

        // Act
        bool found = n.Exists(7);

        // Assert
        Assert.IsTrue(found);
    }

    [TestMethod]
    public void Exists_SingleNodeMissing_ReturnsFalse()
    {
        // Arrange
        Node<int> n = new Node<int>(7);

        // Act
        bool found = n.Exists(8);

        // Assert
        Assert.IsFalse(found);
    }

    [TestMethod]
    public void Exists_NullReferenceValue_ReturnsTrue()
    {
        // Arrange
        Node<string?> n = new Node<string?>(null);
        n.Append("x");

        // Act
        bool found = n.Exists(null);

        // Assert
        Assert.IsTrue(found);
    }

    [TestMethod]
    public void ChildItems_MaxGreaterThanChildCount_ReturnsAllChildItems()
    {
        // Arrange
        Node<int> n1 = new Node<int>(1);
        n1.Append(2);
        n1.Append(3);

        // Act
        List<int> result = n1.ChildItems(5).ToList();

        // Assert
        CollectionAssert.AreEqual(new List<int> { 3, 2 }, result);
    }

    [TestMethod]
    public void ChildItems_MaxLessThanChildCount_ReturnsUpToMaximum()
    {
        // Arrange
        Node<int> n1 = new Node<int>(1);
        n1.Append(2);
        n1.Append(3);
        n1.Append(4);
        n1.Append(5);

        // Act
        List<int> result = n1.ChildItems(2).ToList();

        // Assert
        CollectionAssert.AreEqual(new List<int> { 5, 4 }, result);
    }

    [TestMethod]
    public void ChildItems_MaximumZero_ReturnsEmptyCollection()
    {
        // Arrange
        Node<int> n1 = new Node<int>(1);
        n1.Append(2);
        n1.Append(3);
        
        // Act
        List<int> result = n1.ChildItems(0).ToList();
       
        // Assert
        CollectionAssert.AreEqual(new List<int> { }, result);
    }
}