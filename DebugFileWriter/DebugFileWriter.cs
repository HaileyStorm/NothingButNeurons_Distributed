using Proto;
using System;
using System.IO;

namespace NothingButNeurons.DebugFileWriter;

/// <summary>
/// The DebugFileWriter class is responsible for writing debug messages to a log file.
/// </summary>
internal class DebugFileWriter : NodeBase
{
    private string FileName;

    /// <summary>
    /// Initializes a new instance of the DebugFileWriter class.
    /// </summary>
    /// <param name="fileName">The name of the log file. Default is "log.txt".</param>
    public DebugFileWriter(PID? debugServerPID, string fileName = "log.txt") : base(debugServerPID, "DebugFileWriter")
    {
        FileName = fileName;
        try
        {
            using FileStream stream = GetWriteStream(FileName, false, 1000);
            using StreamWriter file = new(stream);
            file.Flush();
            file.Dispose();
        } catch (TimeoutException)
        {
            SendDebugMessage(DebugSeverity.Error, "DebugFileWriter", "Failed to create/clear the log file during spawn (IOException/timeout).");
        }
    }

    /// <summary>
    /// Handles incoming messages and processes them accordingly.
    /// </summary>
    /// <param name="context">The context of the message.</param>
    /// <returns>True if the message was processed, false otherwise.</returns>
    protected override bool ReceiveMessage(IContext context)
    {
        // Process base class messages first
        bool processed = base.ReceiveMessage(context);
        if (processed)
            return true;

        switch (context.Message)
        {
            case DebugInboundMessage msg:
                processed = true;

                if (!String.IsNullOrEmpty(msg.Context) && String.Equals(msg.Context, "DebugFileWriter"))
                    break;

                HandleUnstable(context, msg, async (context, msg) =>
                {
                    try
                    {
                        using FileStream stream = GetWriteStream(FileName, true, 1000);
                        using StreamWriter file = new(stream);
                        await file.WriteLineAsync($"{CCSL.DatesAndTimes.UnixTimeToLocalDateTime(msg.MessageSentTime):MM/dd/yyyy hh:mm:ss.fff tt}: {msg}");
                        file.Dispose();
                    } catch (TimeoutException)
                    {
                        SendDebugMessage(DebugSeverity.Warning, "DebugFileWriter", "Failed to write debug message to log file (IOException/timeout).");
                    }
                });
                break;
            case DebugFlushMessage msg:
                processed = true;

                HandleUnstable(context, msg, async (context, msg) =>
                {
                    try
                    {
                        using FileStream stream = GetWriteStream(FileName, false, 1000);
                        using StreamWriter file = new(stream);
                        await file.WriteLineAsync($"{CCSL.DatesAndTimes.UnixTimeToLocalDateTime(DateTimeOffset.Now.ToUnixTimeMilliseconds()):MM/dd/yyyy hh:mm:ss.fff tt}: Log file flushed.");
                        file.Flush();
                        file.Dispose();
                        SendDebugMessage(DebugSeverity.Debug, "DebugFileWriter", "Log file flushed.");
                    } catch (TimeoutException)
                    {
                        SendDebugMessage(DebugSeverity.Warning, "DebugFileWriter", "Failed to flush log file (IOException/timeout).");
                    }
                });

                break;
        }

        return processed;
    }

    protected override void ProcessRestartingMessage(IContext context, Restarting msg)
    {
    }

    protected override void ProcessStartedMessage(IContext context, Started msg)
    {
        SubscribeToDebugs(DebugSeverity.Info);
    }

    protected override void ProcessStoppingMessage(IContext context, Stopping msg)
    {
    }

    protected override void ProcessStoppedMessage(IContext context, Stopped msg)
    {
    }

    /// <summary>
    /// Attempts to acquire a write handle to the specified file for writing or appending.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="append">Whether to append to the existing file or overwrite it.</param>
    /// <param name="timeoutMs">The maximum time to wait (in milliseconds) before throwing a TimeoutException.</param>
    /// <returns>A FileStream for writing to the specified file.</returns>
    /// <exception cref="TimeoutException">Thrown if a write handle cannot be acquired within the specified timeout.</exception>
    private FileStream GetWriteStream(string path, bool append, int timeoutMs)
    {
        var time = Stopwatch.StartNew();
        while (time.ElapsedMilliseconds < timeoutMs)
        {
            try
            {
                return new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write);
            }
            catch (IOException e)
            {
                // access error
                if (e.HResult != -2147024864)
                    throw;
            }
        }

        throw new TimeoutException($"Failed to get a write handle to {path} within {timeoutMs}ms.");
    }
}
