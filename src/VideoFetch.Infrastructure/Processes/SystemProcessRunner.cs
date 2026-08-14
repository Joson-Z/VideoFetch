using System.Diagnostics;
using System.Text;
using VideoFetch.Application.Processes;

namespace VideoFetch.Infrastructure.Processes;

public sealed class SystemProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        ProcessRequest request,
        IProgress<ProcessOutputLine>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FileName);

        using CancellationTokenSource? timeoutSource = request.Timeout is { } timeout
            ? new CancellationTokenSource(timeout)
            : null;
        using CancellationTokenSource linkedSource = timeoutSource is null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        ProcessStartInfo startInfo = new()
        {
            FileName = request.FileName,
            WorkingDirectory = request.WorkingDirectory ?? Environment.CurrentDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (string argument in request.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using Process process = new() { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException($"无法启动进程：{request.FileName}");
        }

        Task<string> outputTask = ReadStreamAsync(process.StandardOutput, false, progress);
        Task<string> errorTask = ReadStreamAsync(process.StandardError, true, progress);

        try
        {
            await process.WaitForExitAsync(linkedSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKillProcessTree(process);
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);

            if (timeoutSource?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException("进程执行超时。", timeoutSource.Token);
            }

            throw;
        }

        string standardOutput = await outputTask.ConfigureAwait(false);
        string standardError = await errorTask.ConfigureAwait(false);
        return new ProcessResult(process.ExitCode, standardOutput, standardError);
    }

    private static async Task<string> ReadStreamAsync(
        StreamReader reader,
        bool isError,
        IProgress<ProcessOutputLine>? progress)
    {
        StringBuilder buffer = new();
        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            buffer.AppendLine(line);
            progress?.Report(new ProcessOutputLine(line, isError));
        }

        return buffer.ToString();
    }

    private static void TryKillProcessTree(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // The process exited between the state check and Kill.
        }
    }
}
