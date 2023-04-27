global using System.Diagnostics;
using NothingButNeurons.Shared.Messages;
using Proto;

namespace NothingButNeurons.Shared;

/// <summary>
/// Base class for message types used by ActorBase.
/// </summary>
public abstract record Message;

/// <summary>
/// Delegate for handling unstable messages without return values.
/// </summary>
/// <typeparam name="TUnstableMessage">The type of unstable message.</typeparam>
public delegate void UnstableMessageHandler<TUnstableMessage>(IContext context, TUnstableMessage msg);

/// <summary>
/// Delegate for handling unstable messages with return values.
/// </summary>
/// <typeparam name="TUnstableMessage">The type of unstable message.</typeparam>
/// <typeparam name="TUnstableReturn">The type of the return value.</typeparam>
public delegate TUnstableReturn UnstableMessageHandlerWithReturn<TUnstableMessage, TUnstableReturn>(IContext context, TUnstableMessage msg);

/// <summary>
/// Represents an exception that occurs when handling an unstable message.
/// </summary>
public record UnstableHandlerException(Message FailedMessage) : Message;

/// <summary>
/// Base class for actors in the NothingButNeurons project.
/// </summary>
public abstract class ActorBase : IActor
{
    private IContext? BaseContext { get; set; }
    protected virtual PID? SelfPID { get; set; }
    protected virtual PID? ParentPID { get; set; }
    protected virtual PID? DebugServerPID { get; set; }


    #region ReceiveAsync Method
    /// <summary>
    /// Handles incoming messages and forwards them to the appropriate behavior methods.
    /// </summary>
    /// <returns>True if the message was successfully processed, false otherwise.</returns>
    public virtual Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            // Process the Started message and initialize properties
            case Started msg:
                BaseContext = context;
                SelfPID = context.Self;
                ParentPID = context.Parent;
                DebugServerPID = new PID(context.System.Address, "DebugServer");
                _ProcessStartedMessage(context, msg);
                ProcessStartedMessage(context, msg);
                SendDebugMessage(DebugSeverity.Trace, "Spawn", $"{SelfPID.Address}/{SelfPID.Id} spawned.");
                break;

            // Process the Restarting message
            case Restarting msg:
                SendDebugMessage(DebugSeverity.Trace, "Restart", $"{SelfPID!.Address}/{SelfPID.Id} restarted.");
                ProcessRestartingMessage(context, msg);
                break;

            // Process the Stopping message
            case Stopping msg:
                SendDebugMessage(DebugSeverity.Trace, "Stopping", $"{SelfPID!.Address}/{SelfPID.Id} is stopping.");
                ProcessStoppingMessage(context, msg);
                break;

            // Process the Stopped message
            case Stopped msg:
                SendDebugMessage(DebugSeverity.Trace, "Stopped", $"{SelfPID!.Address}/{SelfPID.Id} stopped.");
                ProcessStoppedMessage(context, msg);
                break;

            // Process the UnstableHandlerException message
            case UnstableHandlerException msg:
                SendDebugMessage(DebugSeverity.Alert, "UnstableHandlerException", "InputNeuron Received UnstableHandler Exception:", msg.FailedMessage.ToString());
                // Still want descendants to be able to process the exception
                ReceiveMessage(context);
                break;

            // Process other messages
            default:
                ReceiveMessage(context);
                break;
        }

        return Task.CompletedTask;
    }
    #endregion

    // Abstract method to be implemented in derived classes for handling messages
    protected abstract bool ReceiveMessage(IContext context);

    // Lifecycle methods
    protected virtual void _ProcessStartedMessage(IContext context, Started msg) { }
    protected abstract void ProcessStartedMessage(IContext context, Started msg);
    protected abstract void ProcessRestartingMessage(IContext context, Restarting msg);
    protected abstract void ProcessStoppingMessage(IContext context, Stopping msg);
    protected abstract void ProcessStoppedMessage(IContext context, Stopped msg);

    #region Unstable Message Handling
    /// <summary>
    /// Forwards an unstable message to a child actor for processing.
    /// </summary>
    protected virtual void ForwardUnstable<TUnstableMessage>(IContext context, UnstableMessageHandler<TUnstableMessage> handler) where TUnstableMessage : Message
    {
        // TODO: If context.Message is not TUnstableMessage, send debug message
        PID child = context.Spawn(Props.FromProducer(() => new UnstableActor<TUnstableMessage>(handler)));
        context.Forward(child);
        context.Poison(child);
    }
    /// <summary>
    /// Forwards an unstable message with a return value to a child actor for processing.
    /// </summary>
    protected virtual void ForwardUnstable<TUnstableMessage, TUnstableReturn>(IContext context, UnstableMessageHandlerWithReturn<TUnstableMessage, TUnstableReturn> handler) where TUnstableMessage : Message where TUnstableReturn : Message
    {
        // TODO: If context.Message is not TUnstableMessage, send debug message
        PID child = context.Spawn(Props.FromProducer(() => new UnstableActorWithReturn<TUnstableMessage, TUnstableReturn>(handler)));
        context.Forward(child);
        context.Poison(child);
    }
    
    protected virtual void HandleUnstable<TUnstableMessage>(IContext context, TUnstableMessage msg, UnstableMessageHandler<TUnstableMessage> handler) where TUnstableMessage : Message
    {
        PID child = context.Spawn(Props.FromProducer(() => new UnstableActor<TUnstableMessage>(handler)));
        context.Send(child, msg);
        context.Poison(child);
    }
    protected virtual void HandleUnstable<TUnstableMessage, TUnstableReturn>(IContext context, TUnstableMessage msg, UnstableMessageHandlerWithReturn<TUnstableMessage, TUnstableReturn> handler) where TUnstableMessage : Message where TUnstableReturn : Message
    {
        PID child = context.Spawn(Props.FromProducer(() => new UnstableActorWithReturn<TUnstableMessage, TUnstableReturn>(handler)));
        context.Send(child, msg);
        context.Poison(child);
    }
    #endregion

    #region Unstable Message Handling
    protected virtual void SubscribeToDebugs(DebugSeverity severity = DebugSeverity.Trace, string context = "")
    {
        if (BaseContext == null || DebugServerPID == null || SelfPID == null)
        {
            SendDebugMessage(DebugSeverity.Warning, "Spawn(?)", "SubscribeToDebugs called before startup", "BaseContext or DebugServerPID or SelfPID null.");
            return;
        }
        BaseContext.Send(DebugServerPID, new DebugSubscribeMessage(SelfPID, severity, context));
    }
    protected virtual void UnsubscribeFromDebugs()
    {
        if (BaseContext == null || DebugServerPID == null || SelfPID == null)
        {
            SendDebugMessage(DebugSeverity.Warning, "Spawn(?)", "UnsubscribeFromDebugs called before startup", "BaseContext or DebugServerPID or SelfPID null.");
            return;
        }
        BaseContext.Send(DebugServerPID, new DebugUnsubscribeMessage(SelfPID));
    }
    protected virtual void SendDebugMessage(DebugOutboundMessage msg)
    {
        if (BaseContext == null || DebugServerPID == null)
        {
            SendDebugMessage(DebugSeverity.Warning, "Spawn(?)", "SendDebugMessage called before startup", "BaseContext or DebugServerPID null.");
            return;
        }
        BaseContext.Send(DebugServerPID, msg);
    }
    protected virtual void SendDebugMessage(DebugSeverity severity = DebugSeverity.Trace, string context = "", string summary = "", string message = "")
    {
        string senderClass = this.GetType().ToString();
        string senderName = SelfPID == null ? "" : SelfPID.Id;
        string senderSystemAddr = SelfPID == null ? "" : SelfPID.Address;
        string parentName = ParentPID == null ? "" : ParentPID.Id;
        string parentSystemAddr = ParentPID == null ? "" : ParentPID.Address;
        SendDebugMessage(new DebugOutboundMessage(severity, context, summary, message, senderClass, senderName, senderSystemAddr, parentName, parentSystemAddr, DateTimeOffset.Now.ToUnixTimeMilliseconds()));
    }
    #endregion
}


/// <summary>
/// An actor that handles unstable messages without return values.
/// </summary>
internal class UnstableActor<TUnstableMessage> : IActor where TUnstableMessage : Message
{
    private UnstableMessageHandler<TUnstableMessage> Handler { get; init; }
    
    public UnstableActor(UnstableMessageHandler<TUnstableMessage> handler) => Handler = handler;

    /// <summary>
    /// Handles incoming messages and processes unstable messages.
    /// </summary>
    public virtual Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Restarting:
                break;
            case TUnstableMessage msg:
                try
                {
                    Handler(context, msg);
                } catch
                {
                    context.Send(context.Parent!, new UnstableHandlerException(msg));
                }
                break;
        }
        return Task.CompletedTask;
    }
}

#region Unstable Actor Classes
/// <summary>
/// An actor that handles unstable messages with return values.
/// </summary>
internal class UnstableActorWithReturn<TUnstableMessage, TUnstableReturn> : IActor where TUnstableMessage : Message where TUnstableReturn : Message
{
    private UnstableMessageHandlerWithReturn<TUnstableMessage, TUnstableReturn> Handler { get; init; }

    public UnstableActorWithReturn(UnstableMessageHandlerWithReturn<TUnstableMessage, TUnstableReturn> handler) => Handler = handler;

    /// <summary>
    /// Handles incoming messages and processes unstable messages with return values.
    /// </summary>
    public virtual Task ReceiveAsync(IContext context)
    {
        switch (context.Message)
        {
            case Restarting:
                break;
            case TUnstableMessage msg:
                try
                {
                    TUnstableReturn ret = Handler(context, msg);
                    context.Send(context.Parent!, ret);
                } catch
                {
                    context.Send(context.Parent!, new UnstableHandlerException(msg));
                }
                break;
        }
        return Task.CompletedTask;
    }
}
#endregion