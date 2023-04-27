using Proto;
using Proto.Router;
using NothingButNeurons.Shared.Messages;

namespace NothingButNeurons.Shared;

/// <summary>
/// Represents an abstract base class for actors with broadcasting functionality.
/// Inherits from <see cref="ActorBase"/>.
/// </summary>
public abstract class ActorBaseWithBroadcaster : ActorBase
{
    private PID? RouterPID;
    /// <summary>
    /// Gets or sets the base context for the actor.
    /// </summary>
    private IContext? BaseContext { get; set; }

    /// <summary>
    /// Handles the <see cref="Started"/> message and initializes the RouterPID and BaseContext properties.
    /// </summary>
    /// <param name="context">The actor's context.</param>
    /// <param name="msg">The started message.</param>
    protected override void _ProcessStartedMessage(IContext context, Started msg)
    {
        RouterPID = context.SpawnNamed(context.NewBroadcastGroup(Array.Empty<PID>()), "Broadcaster");
        BaseContext = context;
    }

    /// <summary>
    /// Adds a routee to the router.
    /// </summary>
    /// <param name="routee">The PID of the routee to be added.</param>
    protected virtual void AddRoutee(PID routee)
    {
        if (BaseContext == null || RouterPID == null)
        {
            Debug.WriteLine($"Actor {(SelfPID == null ? "null" : SelfPID.Address + "/" + SelfPID.Id)} received AddRoutee request before startup");
            SendDebugMessage(DebugSeverity.Warning, "Spawn(?)", "Actor received AddRoutee request before startup", "BaseContext and/or RouterPID null.");
            return;
        }
            
        BaseContext.Send(RouterPID, new Proto.Router.Messages.RouterAddRoutee(routee));
    }

    /// <summary>
    /// Broadcasts a message to all routees.
    /// </summary>
    /// <param name="msg">The message to be broadcasted.</param>
    protected virtual void Broadcast(Message msg)
    {
        if (BaseContext == null || RouterPID == null)
        {
            Debug.WriteLine($"Actor {(SelfPID == null ? "null" : SelfPID.Address + "/" + SelfPID.Id)} received Broadcast request before startup");
            SendDebugMessage(DebugSeverity.Warning, "Spawn(?)", "Actor received Broadcast request before startup", "BaseContext and/or RouterPID null.");
            return;
        }

        //Debug.WriteLine($"Actor {(SelfPID == null ? "null" : SelfPID.Address + "/" + SelfPID.Id)} BROADCASTING: {msg.GetType()}.");
        //BaseContext.RequestReenter<object>(RouterPID, new Proto.Router.Messages.RouterGetRoutees(), (Task<object> t) => { dynamic r = t.Result;  Debug.WriteLine($"GetRoutees returned:"); foreach (Proto.PID pid in r.Pids) { Debug.WriteLine($"\t{pid}"); }; return null; }, new System.Threading.CancellationToken());
        BaseContext.Send(RouterPID, msg);
    }
}
