using Proto;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NothingButNeurons.Debugger;

/// <summary>
/// Manages subscriptions and processes incoming/outgoing debug messages.
/// </summary>
internal class DebugServer : ActorBase
{
    // Dictionary of subscribers and their subscription settings
    private Dictionary<PID, DebugSubscribeMessage> Subscribers;

    public DebugServer() : base(new PID())
    {
        Subscribers = new Dictionary<PID, DebugSubscribeMessage>();
    }

    /// <summary>
    /// Processes received messages.
    /// </summary>
    /// <param name="context">The context of the received message.</param>
    /// <returns>true if the message was processed, false otherwise.</returns>
    protected override bool ReceiveMessage(IContext context)
    {
        bool processed = false;

        switch (context.Message)
        {
            case DebugSubscribeMessage msg:
                if (msg.Subscriber != null) Subscribers[msg.Subscriber] = msg;
                break;
            case DebugUnsubscribeMessage msg:
                if (msg.Subscriber != null) Subscribers.Remove(msg.Subscriber);
                break;
            case DebugOutboundMessage msg:
                // Send DebugInboundMessage to subscribers based on their subscription settings
                Regex regex;
                foreach (var subscriber in Subscribers)
                {
                    regex = new Regex(subscriber.Value.Context);
                    if (msg.Severity >= subscriber.Value.Severity && (string.IsNullOrEmpty(subscriber.Value.Context) || regex.IsMatch(msg.Context)))
                    {
                        context.Send(subscriber.Key, msg.AsInbound(DateTimeOffset.Now.ToUnixTimeMilliseconds()));
                    }
                }
                break;
            case DebugFlushAllMessage:
                // Send DebugFlushMessage to all subscribers
                foreach (var subscriber in Subscribers)
                {
                    context.Send(subscriber.Key, new DebugFlushMessage());
                }
                break;
        }

        return processed;
    }

    protected override void ProcessRestartingMessage(IContext context, Restarting msg)
    {
        
    }

    protected override void ProcessStartedMessage(IContext context, Started msg)
    {
    }

    protected override void ProcessStoppingMessage(IContext context, Stopping msg)
    {
    }

    protected override void ProcessStoppedMessage(IContext context, Stopped msg)
    {
    }
}
