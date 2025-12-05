using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Assignment.Tests;

internal class PingProcessMock : PingProcess
{
    public int ExitCode { get; set; } = 0;
    public string StdOutput { get; set; } = string.Empty;
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;
    public Exception? ExceptionToThrow { get; set; }
    public ConcurrentQueue<string?> ProgressLines { get; } = new();
    public ProcessStartInfo? LastStartInfo { get; private set; }

    public void Reset()
    {
        ProgressLines.Clear();
        LastStartInfo = null;
        ExceptionToThrow = null;
        Delay = TimeSpan.Zero;
        ExitCode = 0;
        StdOutput = string.Empty;
    }
    public new Assignment.PingResult Run(string hostNameOrAddress)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        if (Delay > TimeSpan.Zero)
            Thread.Sleep(Delay);

        return new Assignment.PingResult(ExitCode, StdOutput);
    }
    public new Task<Assignment.PingResult> RunTaskAsync(string hostNameOrAddress)
    {
        return RunAsync(hostNameOrAddress);
    }

    public new async Task<Assignment.PingResult> RunAsync(string hostNameOrAddress, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        if (cancellationToken.IsCancellationRequested)
            return await Task.FromCanceled<Assignment.PingResult>(cancellationToken);

        if (Delay > TimeSpan.Zero)
            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

        return new Assignment.PingResult(ExitCode, StdOutput);
    }
    public new async Task<Assignment.PingResult> RunAsync(params string[] hostNameOrAddresses)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        if (Delay > TimeSpan.Zero)
            await Task.Delay(Delay).ConfigureAwait(false);
        var sb = new System.Text.StringBuilder();
        foreach (var h in hostNameOrAddresses)
        {
            sb.AppendLine($"Mock ping for {h}");
            sb.AppendLine(StdOutput);
        }

        return new Assignment.PingResult(ExitCode, sb.ToString());
    }
    public new async Task<Assignment.PingResult> RunLongRunningAsync(string hostNameOrAddress, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        if (cancellationToken.IsCancellationRequested)
            return await Task.FromCanceled<Assignment.PingResult>(cancellationToken);

        if (Delay > TimeSpan.Zero)
            await Task.Delay(Delay, cancellationToken).ConfigureAwait(false);

        return new Assignment.PingResult(ExitCode, StdOutput);
    }
    public new Task<int> RunLongRunningAsync(ProcessStartInfo startInfo, Action<string?>? progressOutput, Action<string?>? progressError, CancellationToken token)
    {
        LastStartInfo = startInfo;

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        if (token.IsCancellationRequested)
            return Task.FromCanceled<int>(token);

        progressOutput?.Invoke(StdOutput);
        ProgressLines.Enqueue(StdOutput);

        if (Delay > TimeSpan.Zero)
            return Task.Delay(Delay).ContinueWith(_ => ExitCode, token);

        return Task.FromResult(ExitCode);
    }
    public new Task<Assignment.PingResult> RunAsync(string hostNameOrAddress, IProgress<string?> progress, CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<Assignment.PingResult>(cancellationToken);
        progress?.Report(StdOutput);
        ProgressLines.Enqueue(StdOutput);

        if (Delay > TimeSpan.Zero)
            return Task.Delay(Delay, cancellationToken).ContinueWith(_ => new Assignment.PingResult(ExitCode, StdOutput), cancellationToken);

        return Task.FromResult(new Assignment.PingResult(ExitCode, StdOutput));
    }
}
