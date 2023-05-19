using Proto;
using Proto.Router;
using NothingButNeurons.Shared.Messages;

namespace NothingButNeurons.Shared;

/// <summary>
/// Represents an abstract base class for actors with broadcasting functionality.
/// Inherits from <see cref="ActorBase"/>.
/// </summary>
public abstract class NodeBase : ActorBaseWithBroadcaster
{
    protected NodeBase(PID? debugServerPID) : base(debugServerPID) { }

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
        base._ProcessStartedMessage(context, msg);
        
        // TODO: Node stuff. Like setting up to watch for own port setting change or DebugServer PID change.
    }
}
