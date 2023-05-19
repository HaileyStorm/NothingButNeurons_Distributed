using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Designer;

public class DesignerHelper : NodeBase
{
    private bool _processed;

    public DesignerHelper() : base(null, "Designer") { }

    /// <summary>
    /// Overrides the ReceiveMessage method from ActorBaseWithBroadcaster, handling messages based on the current behavior.
    /// </summary>
    /// <param name="context">The actor context.</param>
    /// <returns>True if the message was processed, otherwise false.</returns>
    /*protected override bool ReceiveMessage(IContext context)
    {
        // Process base class messages first
        bool processed = base.ReceiveMessage(context);
        if (processed)
                    return true;

        return processed;
    }*/

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
