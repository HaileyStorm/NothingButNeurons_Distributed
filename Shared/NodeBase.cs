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
    string NodeName { get; init; }
    protected NodeBase(PID? debugServerPID, string nodeName) : base(debugServerPID) {
        NodeName = nodeName;
    }

    protected override bool ReceiveMessage(IContext context)
    {
        bool processed = false;

        switch (context.Message)
        {
            case SettingChangedMessage msg:            
                if (string.Equals(msg.TableName, "NodeStatus", StringComparison.InvariantCultureIgnoreCase) && string.Equals(msg.Setting, "DebugServer", StringComparison.InvariantCultureIgnoreCase))
                {
                    context.System.EventStream.Publish(new DebugServerChangedMessage() { PID = Nodes.GetPIDFromString(msg.Value) });
                    processed = true;
                }

                if (string.Equals(msg.TableName, "Ports", StringComparison.InvariantCultureIgnoreCase) && string.Equals(msg.Setting, NodeName, StringComparison.InvariantCultureIgnoreCase))
                {
                    context.System.EventStream.Publish(new SelfPortChangedMessage() { Port = int.Parse(msg.Value) });
                    processed = true;
                }

                break;
        }

        return processed;
    }

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
