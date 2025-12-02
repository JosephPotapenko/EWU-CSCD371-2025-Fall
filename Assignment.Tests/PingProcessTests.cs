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
    PingProcess Sut { get; set; } = new();

    [TestInitialize]
    public void TestInitialize()
    {
        Sut = new();
    }

    private static ProcessStartInfo CreatePingStartInfo(string host, int count = 1)
    {
        if (OperatingSystem.IsWindows())
        {
            return new ProcessStartInfo("ping", host)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
        else
        {
            return new ProcessStartInfo("ping", $"-c {count} {host}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
    }

    [TestMethod]
    public void Start_PingProcess_Success()
    {
        var psi = CreatePingStartInfo("localhost", 1);
        using Process? process = Process.Start(psi);
        Assert.IsNotNull(process, "Failed to start ping process");
        string output = process!.StandardOutput.ReadToEnd();
        process.WaitForExit();
        Assert.AreEqual<int>(0, process.ExitCode, $"ping failed; output: {output}");
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
    public void RunAsync_UsingTplWithCancellation_CatchAggregateExceptionWrapping()
    {
        using CancellationTokenSource cts = new();
        Task<PingResult> task = Sut.RunAsync("localhost", cts.Token);
        cts.Cancel();

        Assert.Throws<AggregateException>(() => { _ = task.Result; });
    }

    [TestMethod]
    public void RunAsync_UsingTplWithCancellation_CatchAggregateExceptionWrappingTaskCanceledException()
    {
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
        int perHostLines = PingOutputLikeExpression.Split(Environment.NewLine).Length;
        int expectedMinimumLines = perHostLines * hostNames.Length;

        PingResult result = await Sut.RunAsync(hostNames);
        int? lineCount = result.StdOutput?.Split(Environment.NewLine).Length;
        Assert.IsTrue(lineCount.HasValue && lineCount.Value >= expectedMinimumLines,
            $"Expected at least {expectedMinimumLines} lines but got {lineCount ?? 0}. Output: {result.StdOutput}");
    }

    [TestMethod]
    async public Task RunLongRunningAsync_UsingTpl_Success()
    {
        PingResult result = await Sut.RunLongRunningAsync("localhost");
        AssertValidPingOutput(result);
    }

    [TestMethod]
    public void RunLongRunningAsync_WithCallbacksAndStartInfo_Success()
    {
        var startInfo = CreatePingStartInfo("localhost", 1);
        List<string?> output = new();
        int exitCode = Sut.RunLongRunningAsync(startInfo, line => output.Add(line), null!, CancellationToken.None).Result;
        string stdOutput = string.Join(Environment.NewLine, output.Where(l => l is not null));
        AssertValidPingOutput(exitCode, stdOutput);
    }

    [TestMethod]
    public void RunLongRunningAsync_WithCancellation_AggregateException()
    {
        var startInfo = CreatePingStartInfo("localhost", 1);
        using var cts = new CancellationTokenSource();
        var task = Sut.RunLongRunningAsync(startInfo, _ => { }, null!, cts.Token);
        cts.Cancel();

        Assert.Throws<AggregateException>(() => { _ = task.Result; });
    }

    [TestMethod]
    public void RunAsync_WithProgress_ReportsLines_Success()
    {
        List<string?> lines = new();
        var progress = new Progress<string?>(line => lines.Add(line));
        PingResult result = Sut.RunAsync("localhost", progress).Result;
        Assert.IsNotEmpty(lines);
        AssertValidPingOutput(result);
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
