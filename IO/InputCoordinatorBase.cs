using Proto;

namespace NothingButNeurons.Shared;

/// <summary>
/// The InputCoordinator is responsible for processing input data from the external world
/// and forwarding it to the appropriate brain regions and neurons. This class is intended
/// to act as a bridge between the external world and the internal structure of the neural
/// network, ensuring that input data flows correctly.
/// </summary>
/// <remarks>
/// The current implementation is not complete and serves as a placeholder for future development.
/// The intended structure is: World <-> HiveMind <-> Brain(<-> Coordinator <->) Region <-> Neuron,
/// where Coordinators are used only for input/output processing.
/// </remarks>
public abstract class InputCoordinatorBase : ActorBaseWithBroadcaster
{

    /// <summary>
    /// Initializes a new instance of the <see cref="InputCoordinatorBase"/> class.
    /// </summary>
    public InputCoordinatorBase(PID debugServerPID) : base(debugServerPID)
    {

    }

    #region Message Handling
    /// <summary>
    /// Processes incoming messages, including those related to input data processing.
    /// Currently, this method is not implemented and simply throws a NotImplementedException.
    /// </summary>
    /// <param name="context">The actor context.</param>
    /// <returns>True if the message was handled; otherwise, false.</returns>
    /// <exception cref="NotImplementedException">Thrown since the method is not implemented yet.</exception>
    protected override bool ReceiveMessage(IContext context)
    {
        //throw new NotImplementedException();
        return false;
    }
    #endregion

    #region Lifecycle Event Handlers

    // These methods are currently empty, serving as placeholders for future implementation.
    // If required, they can be used to handle specific actions during the actor's lifecycle events.
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
    #endregion
}
