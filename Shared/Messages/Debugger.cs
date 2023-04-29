using Proto;

namespace NothingButNeurons.Shared.Messages;


public static class DebugMessageExtensions {
    public static string ToMsgString(this DebugOutboundMessage message)
    {
        return $"DebugOutboundMessage {{ Severity = {message.Severity}, Context = {message.Context}, Summary = {message.Summary}, Message = {message.Message}, SenderClass = {message.SenderClass}, SenderName = {message.SenderName}, SenderSystemAddr = {message.SenderSystemAddr}, ParentName = {message.ParentName}, ParentSystemAddr = {message.ParentSystemAddr}, MessageSentTime = {CCSL.DatesAndTimes.UnixTimeToLocalDateTime(message.MessageSentTime):MM/dd/yyyy hh:mm:ss.fff tt} }}";
    }

    public static DebugInboundMessage AsInbound(this DebugOutboundMessage outbound, long serverReceivedTime)
    {
        DebugInboundMessage inbound = new DebugInboundMessage();
        inbound.Severity = outbound.Severity;
        inbound.Context = outbound.Context;
        inbound.Summary = outbound.Summary;
        inbound.Message = outbound.Message;
        inbound.SenderClass = outbound.SenderClass;
        inbound.SenderName = outbound.SenderName;
        inbound.SenderSystemAddr = outbound.SenderSystemAddr;
        inbound.ParentName = outbound.ParentName;
        inbound.ParentSystemAddr = outbound.ParentSystemAddr;
        inbound.MessageSentTime = outbound.MessageSentTime;

        inbound.ServerReceivedTime = serverReceivedTime;

        return inbound;
    }
}