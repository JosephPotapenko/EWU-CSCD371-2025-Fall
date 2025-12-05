using IntelliTect.TestTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Assignment.Tests;

[TestClass]
public class PingProcessTests
{
    PingProcessMock Sut { get; set; } = new();

    [TestInitialize]
    public void TestInitialize()
    {
        Sut = new();
    }

    [TestMethod]
    public void Run_GoogleDotCom_Success()
    {
        int exitCode = Sut.Run("google.com").ExitCode;
        Assert.AreEqual<int>(0, exitCode);
    }


    [TestMethod]
    public void Run_InvalidAddressOutput_Success()
    {
        (int exitCode, string? stdOutput) = Sut.Run("badaddress");
        Assert.IsFalse(string.IsNullOrWhiteSpace(stdOutput));
        stdOutput = WildcardPattern.NormalizeLineEndings(stdOutput!.Trim());
        Assert.AreEqual<string?>(
            "Ping request could not find host badaddress. Please check the name and try again.".Trim(),
            stdOutput,
            $"Output is unexpected: {stdOutput}");
        Assert.AreEqual<int>(1, exitCode);
    }

    [TestMethod]
    public void Run_CaptureStdOutput_Success()
    {
        PingResult result = Sut.Run("localhost");
        AssertValidPingOutput(result);
    }

    [TestMethod]
    public void RunTaskAsync_Success()
    {
        Task<PingResult> task = Sut.RunTaskAsync("localhost");
        PingResult result = task.Result;
        AssertValidPingOutput(result);
    }

    [TestMethod]
    public void RunAsync_UsingTaskReturn_Success()
    {
        PingResult result = Sut.RunAsync("localhost").Result;
        AssertValidPingOutput(result);
    }

    [TestMethod]
    async public Task RunAsync_UsingTpl_Success()
    {
        PingResult result = await Sut.RunAsync("localhost");
        AssertValidPingOutput(result);
    }


    [TestMethod]
    public async Task RunAsync_UsingTplWithCancellation_TaskCanceledException()
    {
        CancellationTokenSource cts = new();
        Task<PingResult> task = Sut.RunAsync("localhost", cts.Token);
        cts.Cancel();
        
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await task);
    }

    [TestMethod]
    public void RunAsync_UsingTplWithCancellation_CatchAggregateExceptionWrapping()
    {
        CancellationTokenSource cts = new();
        Task<PingResult> task = Sut.RunAsync("localhost", cts.Token);

        cts.Cancel();

        try
        {
            task.Wait();
            Assert.Fail("Expected TaskCanceledException wrapped in AggregateException");
        }
        catch (AggregateException ex)
        {
            ex = ex.Flatten();

            Assert.IsTrue(
                ex.InnerExceptions.Any(e => e is TaskCanceledException),
                "Expected a TaskCanceledException inside AggregateException.");
        };
    }

    [TestMethod]
    async public Task RunAsync_MultipleHostAddresses_True()
    {
        string[] hostNames = new[] { "localhost", "localhost", "localhost", "localhost" };

        int expectedLineCount = PingOutputLikeExpression.Split(Environment.NewLine).Length * hostNames.Length;

        PingResult result = await Sut.RunAsync(hostNames);

        int? lineCount = result.StdOutput?.Split(Environment.NewLine, StringSplitOptions.None).Length;

        Assert.AreEqual(expectedLineCount, lineCount);
    }

    [TestMethod]
    async public Task RunLongRunningAsync_UsingTpl_Success()
    {
        PingResult result = await Sut.RunLongRunningAsync("localhost");
        AssertValidPingOutput(result);
    }

    [TestMethod]
    [Ignore] // Ignored because this test is expected to fail intermittently due to thread safety issues.
    public void StringBuilderAppendLine_InParallel_IsNotThreadSafe()
    {
        IEnumerable<int> numbers = Enumerable.Range(0, short.MaxValue);
        System.Text.StringBuilder stringBuilder = new();
        numbers.AsParallel().ForAll(item => stringBuilder.AppendLine(""));
        int lineCount = stringBuilder.ToString().Split(Environment.NewLine, StringSplitOptions.None).Length;
        Assert.AreNotEqual(lineCount, numbers.Count() + 1);
    }

    readonly string PingOutputLikeExpression = @"
Pinging * with 32 bytes of data:
Reply from ::1: time<*
Reply from ::1: time<*
Reply from ::1: time<*
Reply from ::1: time<*

Ping statistics for ::1:
    Packets: Sent = *, Received = *, Lost = 0 (0% loss),
Approximate round trip times in milli-seconds:
    Minimum = *, Maximum = *, Average = *".Trim();
    private void AssertValidPingOutput(int exitCode, string? stdOutput)
    {
        Assert.IsFalse(string.IsNullOrWhiteSpace(stdOutput));
        stdOutput = WildcardPattern.NormalizeLineEndings(stdOutput!.Trim());
        Assert.IsTrue(stdOutput?.IsLike(PingOutputLikeExpression) ?? false,
            $"Output is unexpected: {stdOutput}");
        Assert.AreEqual<int>(0, exitCode);
    }
    private void AssertValidPingOutput(PingResult result) =>
        AssertValidPingOutput(result.ExitCode, result.StdOutput);
}
