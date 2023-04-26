using Proto;

namespace NothingButNeurons.Shared.Messages;

/// <summary>
/// Represents the severity levels of debug messages.
/// </summary>
public enum DebugSeverity : byte
{
    Trace,
    Info,
    Debug,
    Warning,
    Alert,
    Error,
    Critical,
    Test
}
/// <summary>
/// Base structure for debug messages.
/// </summary>
public abstract record DebugMessage(
    DebugSeverity Severity,
    string Context,
    string Summary,
    string Message,
    string SenderClass,
    string SenderName,
    string SenderSystemAddr,
    string ParentName,
    string ParentSystemAddr,
    long MessageSentTime
    ) : Message;
public record DebugOutboundMessage : DebugMessage
{
    public DebugOutboundMessage(DebugMessage original) : base(original) { }
    public DebugOutboundMessage(DebugSeverity Severity, string Context, string Summary, string Message, string SenderClass, string SenderName, string SenderSystemAddr, string ParentName, string ParentSystemAddr, long MessageSentTime) : base(Severity, Context, Summary, Message, SenderClass, SenderName, SenderSystemAddr, ParentName, ParentSystemAddr, MessageSentTime) { }

    public override string ToString()
    {
        return $"DebugOutboundMessage {{ Severity = {Severity}, Context = {Context}, Summary = {Summary}, Message = {Message}, SenderClass = {SenderClass}, SenderName = {SenderName}, SenderSystemAddr = {SenderSystemAddr}, ParentName = {ParentName}, ParentSystemAddr = {ParentSystemAddr}, MessageSentTime = {CCSL.DatesAndTimes.UnixTimeToLocalDateTime(MessageSentTime):MM/dd/yyyy hh:mm:ss.fff tt} }}";
    }
}
public record DebugInboundMessage : DebugMessage
{
    public long ServerReceivedTime;
    public DebugInboundMessage(DebugMessage original, long serverReceivedTime) : base(original)
    {
        ServerReceivedTime = serverReceivedTime;
    }
    public DebugInboundMessage(DebugSeverity Severity, string Context, string Summary, string Message, string SenderClass, string SenderName, string SenderSystemAddr, string ParentName, string ParentSystemAddr, long MessageSentTime, long serverReceivedTime) : base(Severity, Context, Summary, Message, SenderClass, SenderName, SenderSystemAddr, ParentName, ParentSystemAddr, MessageSentTime)
    {
        ServerReceivedTime = serverReceivedTime;
    }

    public override string ToString()
    {
        return $"DebugInboundMessage {{ Severity = {Severity}, Context = {Context}, Summary = {Summary}, Message = {Message}, SenderClass = {SenderClass}, SenderName = {SenderName}, SenderSystemAddr = {SenderSystemAddr}, ParentName = {ParentName}, ParentSystemAddr = {ParentSystemAddr}, MessageSentTime = {CCSL.DatesAndTimes.UnixTimeToLocalDateTime(MessageSentTime):MM/dd/yyyy hh:mm:ss.fff tt}, ServerReceivedTime = {CCSL.DatesAndTimes.UnixTimeToLocalDateTime(ServerReceivedTime):MM/dd/yyyy hh:mm:ss.fff tt} }}";
    }
}
/// <summary>
/// Represents a request to subscribe to debug messages.
/// </summary>
public record DebugSubscribeMessage(PID Subscriber, DebugSeverity Severity = DebugSeverity.Trace, string Context = "") : Message;
/// <summary>
/// Represents a request to unsubscribe from debug messages.
/// </summary>
public record DebugUnsubscribeMessage(PID Subscriber) : Message;
/// <summary>
/// Represents a request to flush debug messages.
/// </summary>
public record DebugFlushMessage() : Message;
/// <summary>
/// Represents a request to flush all debug messages.
/// </summary>
public record DebugFlushAllMessage() : Message;