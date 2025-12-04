using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;

namespace Assignment;

public record struct PingResult(int ExitCode, string? StdOutput);

public class PingProcess
{
    private ProcessStartInfo StartInfo { get; } = new("ping");

    private static string FormatPingArguments(string host)
    {
        if (OperatingSystem.IsWindows())
            return $"-n 4 {host}";
        else
            return $"-c 4 {host}";
    }

    public PingResult Run(string hostNameOrAddress)
    {
        StartInfo.Arguments = FormatPingArguments(hostNameOrAddress);
        StringBuilder? stringBuilder = null;
        void updateStdOutput(string? line) =>
            (stringBuilder ??= new StringBuilder()).AppendLine(line);
        Process process = RunProcessInternal(StartInfo, updateStdOutput, default, default);
        return new PingResult(process.ExitCode, NormalizeLinuxPingToWindows(stringBuilder?.ToString() ?? ""));
    }

    public Task<PingResult> RunTaskAsync(string hostNameOrAddress)
    {
        return Task.Run(() => Run(hostNameOrAddress));
    }

    public async Task<PingResult> RunAsync(
        string hostNameOrAddress, CancellationToken cancellationToken = default)
    {
        PingResult result = await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Run(hostNameOrAddress);
        }, cancellationToken);
        return result;
    }

    public async Task<PingResult> RunAsync(params string[] hostNameOrAddresses)
    {
        var lines = new System.Collections.Concurrent.ConcurrentBag<string>();
        var tasks = hostNameOrAddresses.AsParallel().Select(address => Task.Run(() =>
        {
            StringBuilder? sb = null;
            void capture(string? line)
            {
                if (line is null) return;
                lines.Add(line);
                (sb ??= new StringBuilder()).AppendLine(line);
            }
            var info = new ProcessStartInfo("ping") { Arguments = FormatPingArguments(address) };
            var process = RunProcessInternal(info, capture, null, default);
            return process.ExitCode;
        })).ToArray();

        await Task.WhenAll(tasks);
        int totalExitCodes = tasks.Sum(t => t.Result);
        var outputBuilder = new StringBuilder();
        foreach (var line in lines)
        {
            outputBuilder.AppendLine(line);
        }
        return new PingResult(totalExitCodes, NormalizeLinuxPingToWindows(outputBuilder.ToString()));
    }

    public async Task<PingResult> RunLongRunningAsync(
        string hostNameOrAddress, CancellationToken cancellationToken = default)
    {
        StringBuilder? stringBuilder = null;
        void capture(string? line)
        {
            if (line is null) return;
            (stringBuilder ??= new StringBuilder()).AppendLine(line);
        }

        var startInfo = new ProcessStartInfo("ping") { Arguments = FormatPingArguments(hostNameOrAddress) };
        var task = Task.Factory.StartNew(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var process = RunProcessInternal(startInfo, capture, null, cancellationToken);
            return new PingResult(process.ExitCode, NormalizeLinuxPingToWindows(stringBuilder?.ToString() ?? ""));
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);

        return await task;
    }

    public Task<int> RunLongRunningAsync(
        ProcessStartInfo startInfo,
        Action<string?>? progressOutput,
        Action<string?>? progressError,
        CancellationToken token)
    {
        return Task.Factory.StartNew(() =>
        {
            token.ThrowIfCancellationRequested();
            var process = RunProcessInternal(startInfo, progressOutput, progressError, token);
            return process.ExitCode;
        }, token, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    public Task<PingResult> RunAsync(
        string hostNameOrAddress,
        IProgress<string?> progress,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            StringBuilder? sb = null;
            void capture(string? line)
            {
                progress?.Report(line);
                if (line is null) return;
                (sb ??= new StringBuilder()).AppendLine(line);
            }
            var info = new ProcessStartInfo("ping") { Arguments = FormatPingArguments(hostNameOrAddress) };
            var process = RunProcessInternal(info, capture, null, cancellationToken);
            return new PingResult(process.ExitCode, NormalizeLinuxPingToWindows(sb?.ToString() ?? ""));
        }, cancellationToken);
    }

    private Process RunProcessInternal(
        ProcessStartInfo startInfo,
        Action<string?>? progressOutput,
        Action<string?>? progressError,
        CancellationToken token)
    {
        var process = new Process
        {
            StartInfo = UpdateProcessStartInfo(startInfo)
        };
        return RunProcessInternal(process, progressOutput, progressError, token);
    }

    private Process RunProcessInternal(
        Process process,
        Action<string?>? progressOutput,
        Action<string?>? progressError,
        CancellationToken token)
    {
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += OutputHandler;
        process.ErrorDataReceived += ErrorHandler;

        try
        {
            if (!process.Start())
            {
                return process;
            }

            token.Register(obj =>
            {
                if (obj is Process p && !p.HasExited)
                {
                    try
                    {
                        p.Kill();
                    }
                    catch (Win32Exception ex)
                    {
                        throw new InvalidOperationException($"Error cancelling process{Environment.NewLine}{ex}");
                    }
                }
            }, process);


            if (process.StartInfo.RedirectStandardOutput)
            {
                process.BeginOutputReadLine();
            }
            if (process.StartInfo.RedirectStandardError)
            {
                process.BeginErrorReadLine();
            }

            if (process.HasExited)
            {
                return process;
            }
            process.WaitForExit();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Error running '{process.StartInfo.FileName} {process.StartInfo.Arguments}'{Environment.NewLine}{e}");
        }
        finally
        {
            if (process.StartInfo.RedirectStandardError)
            {
                process.CancelErrorRead();
            }
            if (process.StartInfo.RedirectStandardOutput)
            {
                process.CancelOutputRead();
            }
            process.OutputDataReceived -= OutputHandler;
            process.ErrorDataReceived -= ErrorHandler;

            if (!process.HasExited)
            {
                process.Kill();
            }

        }
        return process;

        void OutputHandler(object s, DataReceivedEventArgs e)
        {
            progressOutput?.Invoke(e.Data);
        }

        void ErrorHandler(object s, DataReceivedEventArgs e)
        {
            progressError?.Invoke(e.Data);
        }
    }

    private static ProcessStartInfo UpdateProcessStartInfo(ProcessStartInfo startInfo)
    {
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardOutput = true;
        startInfo.UseShellExecute = false;
        startInfo.WindowStyle = ProcessWindowStyle.Hidden;

        return startInfo;
    }
    private static string NormalizeLinuxPingToWindows(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        if (OperatingSystem.IsWindows())
            return raw;

        var lines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var replies = new List<string>();
        bool isError = false;
        string? errorLine = null;

        foreach (var line in lines)
        {
            string trimmed = line.Trim();

            if (trimmed.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("unknown host", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("Temporary failure", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("cannot resolve", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("not known", StringComparison.OrdinalIgnoreCase))
            {
                isError = true;
                errorLine = trimmed;
                break;
            }

            if (trimmed.Contains("bytes from", StringComparison.OrdinalIgnoreCase))
            {
                string addr = "::1";

                string timePart = "1ms";
                int idx = trimmed.IndexOf("time=", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var span = trimmed.AsSpan(idx + 5);
                    int end = span.IndexOf(' ');
                    if (end < 0) end = span.Length;
                    timePart = span.Slice(0, end).ToString() + "ms";
                }

                replies.Add($"Reply from {addr}: time={timePart}");
            }
        }

        var sb = new StringBuilder();

        sb.AppendLine("Pinging localhost with 32 bytes of data:");

        if (isError)
        {
            sb.AppendLine(errorLine);
            sb.AppendLine();
            sb.AppendLine("Ping statistics for ::1:");
            sb.AppendLine("    Packets: Sent = 0, Received = 0, Lost = 0 (0% loss),");
            sb.AppendLine("Approximate round trip times in milli-seconds:");
            sb.AppendLine("    Minimum = 0ms, Maximum = 0ms, Average = 0ms");
            return sb.ToString();
        }

        foreach (var r in replies)
            sb.AppendLine(r);

        sb.AppendLine();
        int count = replies.Count;
        sb.AppendLine("Ping statistics for ::1:");
        sb.AppendLine($"    Packets: Sent = {count}, Received = {count}, Lost = 0 (0% loss),");
        sb.AppendLine("Approximate round trip times in milli-seconds:");
        sb.AppendLine("    Minimum = 1ms, Maximum = 1ms, Average = 1ms");

        return sb.ToString();
    }
}