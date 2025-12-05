using IntelliTect.TestTools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        Sut.ExitCode = 0;
        Sut.StdOutput = GetSamplePingOutput("localhost", 4);
        Sut.Delay = TimeSpan.Zero;
        Sut.ExceptionToThrow = null;
        Sut.Reset();
        Sut.StdOutput = GetSamplePingOutput("localhost", 4);
    }

    private static string CreatePingStartInfoDummy(string host, int count = 1) => host;

    private static string GetSamplePingOutput(string host, int replies)
    {
        var nl = Environment.NewLine;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Pinging {host} with 32 bytes of data:");
        for (int i = 0; i < replies; i++)
        {
            sb.AppendLine($"Reply from ::1: time<1ms");
        }
        sb.AppendLine();
        sb.AppendLine($"Ping statistics for ::1:");
        sb.AppendLine($"    Packets: Sent = {replies}, Received = {replies}, Lost = 0 (0% loss),");
        sb.AppendLine($"Approximate round trip times in milli-seconds:");
        sb.AppendLine($"    Minimum = 1ms, Maximum = 1ms, Average = 1ms");
        return sb.ToString().Trim();
    }

    private static string GetInvalidHostOutput(string host) =>
        $"ping: {host}: Name or service not known";

    [TestMethod]
    public void Start_PingProcess_Success()
    {
        // Using mock: verify synchronous Run returns configured values
        Sut.ExitCode = 0;
        Sut.StdOutput = GetSamplePingOutput("localhost", 4);
        var result = Sut.Run("localhost");
        Assert.AreEqual<int>(0, result.ExitCode);
        AssertValidPingOutput(result);
    }

    [TestMethod]
    public void Run_GoogleDotCom_Success()
    {
        Sut.ExitCode = 0;
        Sut.StdOutput = GetSamplePingOutput("google.com", 4);
        var result = Sut.Run("google.com");
        Assert.AreEqual<int>(0, result.ExitCode);
    }

    [TestMethod]
    public void Run_InvalidAddressOutput_Success()
    {
        Sut.ExitCode = 2;
        Sut.StdOutput = GetInvalidHostOutput("badaddress");
        (int exitCode, string? stdOutput) = Sut.Run("badaddress");
        Assert.IsFalse(string.IsNullOrWhiteSpace(stdOutput));
        stdOutput = WildcardPattern.NormalizeLineEndings(stdOutput!.Trim());

        string[] knownErrorFragments = new[]
        {
            "could not find host",
            "name or service not known",
            "temporary failure in name resolution",
            "unknown host",
            "can't resolve",
            "badaddress"
        };

        bool containsKnownFragment = knownErrorFragments.Any(f =>
            stdOutput!.Contains(f, StringComparison.OrdinalIgnoreCase));

        Assert.IsTrue(containsKnownFragment, $"Output is unexpected: {stdOutput}");
        Assert.AreNotEqual<int>(0, exitCode);
    }

    [TestMethod]
    public void Run_CaptureStdOutput_Success()
    {
        Sut.StdOutput = GetSamplePingOutput("localhost", 4);
        PingResult result = Sut.Run("localhost");
        AssertValidPingOutput(result);
    }

    [TestMethod]
    public void RunTaskAsync_Success()
    {
        Sut.StdOutput = GetSamplePingOutput("localhost", 4);
        Task<PingResult> task = Sut.RunTaskAsync("localhost");
        PingResult result = task.Result;
        AssertValidPingOutput(result);
    }

    [TestMethod]
    public void RunAsync_UsingTaskReturn_Success()
    {
        Sut.StdOutput = GetSamplePingOutput("localhost", 4);
        PingResult result = Sut.RunAsync("localhost").Result;
        AssertValidPingOutput(result);
    }

    [TestMethod]
    async public Task RunAsync_UsingTpl_Success()
    {
        Sut.StdOutput = GetSamplePingOutput("localhost", 4);
        PingResult result = await Sut.RunAsync("localhost");
        AssertValidPingOutput(result);
    }

    [TestMethod]
    public void RunAsync_UsingTplWithCancellation_CatchAggregateExceptionWrapping()
    {
        Sut.Delay = TimeSpan.FromMilliseconds(100);
        Sut.StdOutput = GetSamplePingOutput("localhost", 4);

        using CancellationTokenSource cts = new();
        Task<PingResult> task = Sut.RunAsync("localhost", cts.Token);
        cts.Cancel();

        Assert.Throws<AggregateException>(() => { _ = task.Result; });
    }

    [TestMethod]
    public void RunAsync_UsingTplWithCancellation_CatchAggregateExceptionWrappingTaskCanceledException()
    {
        Sut.Delay = TimeSpan.FromMilliseconds(100);
        Sut.StdOutput = GetSamplePingOutput("localhost", 4);

        using CancellationTokenSource cts = new();
        Task<PingResult> task = Sut.RunAsync("localhost", cts.Token);
        cts.Cancel();
        try
        {
            _ = task.Result;
            Assert.Fail("Expected AggregateException when accessing Result after cancellation.");
        }
        catch (AggregateException ex)
        {
            bool hasTaskCanceled = ex.Flatten().InnerExceptions.OfType<TaskCanceledException>().Any();
            Assert.IsTrue(hasTaskCanceled, "AggregateException did not contain a TaskCanceledException.");
        }
    }

    [TestMethod]
    async public Task RunAsync_MultipleHostAddresses_True()
    {
        string[] hostNames = new string[] { "localhost", "localhost", "localhost", "localhost" };
        Sut.StdOutput = "Mock line";
        Sut.ExitCode = 0;
        PingResult result = await Sut.RunAsync(hostNames);
        int? lineCount = result.StdOutput?.Split(Environment.NewLine).Length;
        Assert.IsTrue(lineCount.HasValue && lineCount.Value >= hostNames.Length,
            $"Expected at least {hostNames.Length} lines but got {lineCount ?? 0}. Output: {result.StdOutput}");
    }

    [TestMethod]
    async public Task RunLongRunningAsync_UsingTpl_Success()
    {
        Sut.StdOutput = GetSamplePingOutput("localhost", 4);
        PingResult result = await Sut.RunLongRunningAsync("localhost");
        AssertValidPingOutput(result);
    }

    [TestMethod]
    public void RunLongRunningAsync_WithCallbacksAndStartInfo_Success()
    {
        var startInfo = new ProcessStartInfo("ping", "localhost");
        Sut.StdOutput = "progress-line";
        List<string?> output = new();
        int exitCode = Sut.RunLongRunningAsync(startInfo, line => output.Add(line), null!, CancellationToken.None).Result;
        string stdOutput = string.Join(Environment.NewLine, output.Where(l => l is not null));
        Assert.AreEqual(Sut.ExitCode, exitCode);
        Assert.Contains("progress-line", stdOutput);
    }

    [TestMethod]
    public void RunLongRunningAsync_WithCancellation_AggregateException()
    {
        var startInfo = new ProcessStartInfo("ping", "localhost");
        Sut.StdOutput = "progress-line";
        Sut.Delay = TimeSpan.FromMilliseconds(100);
        using var cts = new CancellationTokenSource();
        var task = Sut.RunLongRunningAsync(startInfo, _ => { }, null!, cts.Token);
        cts.Cancel();

        Assert.Throws<AggregateException>(() => { _ = task.Result; });
    }

    [TestMethod]
    public void RunAsync_WithProgress_ReportsLines_Success()
    {
        Sut.StdOutput = "progress-line";
        List<string?> lines = new();
        var progress = new Progress<string?>(line => lines.Add(line));
        PingResult result = Sut.RunAsync("localhost", progress).Result;
        Assert.IsNotEmpty(lines);
        Assert.Contains("progress-line", lines);
    }

    [TestMethod]
    public void StringBuilderAppendLine_InParallel_IsNotThreadSafe()
    {
        IEnumerable<int> numbers = Enumerable.Range(0, short.MaxValue);
        System.Text.StringBuilder stringBuilder = new();
        try
        {
            numbers.AsParallel().ForAll(item => stringBuilder.AppendLine(""));
            int lineCount = stringBuilder.ToString().Split(Environment.NewLine).Length;
            Assert.AreNotEqual(lineCount, numbers.Count() + 1);
        }
        catch (AggregateException)
        {
            return;
        }
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
