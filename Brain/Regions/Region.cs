using NothingButNeurons.Brain.Regions.Neurons;
using Proto;

namespace NothingButNeurons.Brain.Regions;

/// <summary>
/// Represents a message to spawn a neuron.
/// </summary>
public record SpawnNeuronMessage(NeuronAddress Address, AccumulationFunction AccumulationFunction, byte PreActivationThreshold, ActivationFunction ActivationFunction, byte ActivationParameterA, byte ActivationParameterB, byte ActivationThreshold, ResetFunction ResetFunction, int[] Synapses) : Message;
/// <summary>
/// Represents a message returning the spawn neuron request with converted properties.
/// </summary>
public record SpawnNeuronReturnMessage(NeuronAddress Address, AccumulationFunction AccumulationFunction, double PreActivationThreshold, ActivationFunction ActivationFunction, double ActivationParameterA, double ActivationParameterB, double ActivationThreshold, ResetFunction ResetFunction, int[] Synapses) : Message;
/// <summary>
/// Represents a message indicating that neuron spawning has failed.
/// </summary>
public record SpawnNeuronFailedMessage(Message FailedMessage) : Message;
/// <summary>
/// Represents an acknowledgment message for a successful neuron spawn.
/// </summary>
public record SpawnNeuronAckMessage : Message;

/// <summary>
/// The base class for different neural network-like region types.
/// Manages neuron and synapse creation and message processing during runtime.
/// </summary>
internal class Region : ActorBaseWithBroadcaster
{
    // Key properties
    protected RegionAddress Address { get; init; }
    protected bool IsInputRegion { get; init; }
    protected bool IsInteriorRegion { get; init; }
    protected bool IsOutputRegion { get; init; }
    protected PID? InputCoordinator { get; init; }
    protected PID? OutputCoordinator { get; init; }
    protected Dictionary<NeuronAddress, PID> Neurons { get; init; }
    private int AwaitingNeurons { get; set; }
    private int AwaitingSynapseAck { get; set; }

    // Behavior management
    private readonly Behavior _behavior;
    private bool _processed;

    /// <summary>
    /// Initializes a new instance of the <see cref="Region"/> class.
    /// </summary>
    /// <param name="address">The RegionAddress of the region.</param>
    /// <param name="neuronCt">The initial number of neurons to be spawned.</param>
    public Region(RegionAddress address, int neuronCt)
    {
        Address = address;
        // If this is an InputRegion or OutputRegion, these values will be overwritten
        IsInputRegion = false;
        IsInteriorRegion = true;
        IsOutputRegion = false;
        Neurons = new();
        AwaitingNeurons = neuronCt;
        AwaitingSynapseAck = 0;

        _behavior = new Behavior(Spawn);
    }

    #region Behaviours
    /// <summary>
    /// Handles the ReceiveMessage based on the current behavior.
    /// </summary>
    protected override bool ReceiveMessage(IContext context)
    {
        _processed = false;

        _behavior.ReceiveAsync(context).Wait();

        return _processed;
    }

    /// <summary>
    /// The Spawn behavior handles neuron and synapse creation.
    /// </summary>
    private Task Spawn(IContext context)
    {
        // Checks if all neurons and synapses have been processed and changes behavior to Active if true
        void CheckNeuronsAndSynapses()
        {
            if (AwaitingNeurons == 0 && AwaitingSynapseAck == 0)
            {
                _behavior.Become(Active);
            }
        }

        switch (context.Message)
        {
            // Handles the unstable message and converts the properties before creating the neuron
            case SpawnNeuronMessage msg:
                HandleUnstable(context, msg, (context, msg) => new SpawnNeuronReturnMessage(
                    Address: msg.Address,
                    AccumulationFunction: msg.AccumulationFunction,
                    PreActivationThreshold: NeuronBase.BitsToDouble(msg.PreActivationThreshold, 31),
                    ActivationFunction: msg.ActivationFunction,
                    ActivationParameterA: NeuronBase.BitsToDouble(msg.ActivationParameterA, 63, -3d, 3d),
                    ActivationParameterB: NeuronBase.BitsToDouble(msg.ActivationParameterB, 63, -3d, 3d),
                    ActivationThreshold: NeuronBase.BitsToDouble(msg.ActivationThreshold, 15, 0d, 1d),
                    ResetFunction: msg.ResetFunction,
                    Synapses: msg.Synapses
                    ));
                _processed = true;
                break;
            // Spawns the appropriate neuron type based on the region type
            // Adds the neuron to the Neurons dictionary and the router
            case SpawnNeuronReturnMessage msg:
                PID pid;
                if (IsInputRegion)
                {
                    pid = context.SpawnNamed(Props.FromProducer(() => new InputNeuron(msg.Address, msg.Synapses.Length, msg.AccumulationFunction, msg.PreActivationThreshold, msg.ActivationFunction, msg.ActivationParameterA, msg.ActivationParameterB, msg.ActivationThreshold, msg.ResetFunction)), msg.Address.NeuronPart.ToString());
                }
                else if (IsOutputRegion)
                {
                    pid = context.SpawnNamed(Props.FromProducer(() => new OutputNeuron(msg.Address, msg.Synapses.Length, msg.AccumulationFunction, msg.PreActivationThreshold, msg.ActivationFunction, msg.ActivationParameterA, msg.ActivationParameterB, msg.ActivationThreshold, msg.ResetFunction)), msg.Address.NeuronPart.ToString());
                }
                else
                {
                    pid = context.SpawnNamed(Props.FromProducer(() => new InteriorNeuron(msg.Address, msg.Synapses.Length, msg.AccumulationFunction, msg.PreActivationThreshold, msg.ActivationFunction, msg.ActivationParameterA, msg.ActivationParameterB, msg.ActivationThreshold, msg.ResetFunction)), msg.Address.NeuronPart.ToString());
                }

                Neurons[msg.Address] = pid;
                //Debug.WriteLine($"Add neuron routee {pid}");
                AddRoutee(pid);
                AwaitingSynapseAck += msg.Synapses.Length;
                foreach (int synapse in msg.Synapses)
                {
                    context.Send(pid, new SpawnSynapseMessage(synapse));
                }
                context.Send(ParentPID!, new SpawnNeuronAckMessage());
                SendDebugMessage(DebugSeverity.Trace, "Spawn", $"Region {SelfPID!.Address}/{SelfPID!.Id} spawned neuron {pid.Address}/{pid.Id}.", $"Neuron {pid.Address}/{pid.Id} has address {msg.Address.NeuronPart} and has {msg.Synapses.Length} synapses.");
                AwaitingNeurons--;
                if (msg.Synapses.Length == 0) CheckNeuronsAndSynapses();
                _processed = true;
                break;
            // Handles an unstable message exception by sending a SpawnNeuronFailedMessage to the parent
            case UnstableHandlerException msg:
                context.Send(ParentPID!, new SpawnNeuronFailedMessage(msg.FailedMessage));
                AwaitingNeurons--;
                CheckNeuronsAndSynapses();
                _processed = true;
                break;
            case SpawnSynapseFailedMessage msg:
                // TODO: Handle
                SendDebugMessage(DebugSeverity.Alert, "Spawn", "Spawn synapse failed.", msg.FailedMessage.ToString());
                AwaitingSynapseAck--;
                CheckNeuronsAndSynapses();
                _processed = true;
                break;
            case SpawnSynapseAckMessage:
                AwaitingSynapseAck--;
                CheckNeuronsAndSynapses();
                _processed = true;
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// The Active behavior manages message processing during runtime.
    /// </summary>
    private Task Active(IContext context)
    {
        switch (context.Message)
        {
            // Handles the SignalAxonsMessage by forwarding non-local axons to Brain and signaling local axons
            case SignalAxonsMessage msg:
                SendDebugMessage(DebugSeverity.Trace, "Signal", "Region received SignalAxonsMessage", $"{msg.Axons.Length} Axons, potential {msg.Val}. Forwarding signal to in-region neurons and forwarding the rest up to Brain.");
                // Forward the signals for Axons outside this region to the Brain
                Axon[] elsewhere = msg.Axons.Where(axon => axon.ToAddress.RegionPart != Address.Address).ToArray();
                if (elsewhere.Length > 0)
                {
                    //SendDebugMessage(DebugSeverity.Trace, "Signal", $"Forwarding SignalAxonsMessage with {elsewhere.Length} non-local Neurons, potential {msg.Val} to Brain.");
                    context.Send(ParentPID!, new SignalAxonsMessage(msg.Sender, elsewhere, msg.Val));
                }
                // Signal Axons within this region
                IEnumerable<Axon> local = msg.Axons.Where(axon => axon.ToAddress.RegionPart == Address.Address);
                //if (local.Any()) SendDebugMessage(DebugSeverity.Trace, "Signal", $"Region signaling {local.Count()} local Neurons, potential {msg.Val}");
                foreach (Axon axon in local)
                {
                    if (Neurons.TryGetValue(axon.ToAddress, out PID pid))
                    {
                        double str = msg.Val * axon.Strength;
                        SendDebugMessage(DebugSeverity.Trace, "Signal", $"Region {SelfPID!.Address}/{SelfPID!.Id} sending Signal to {pid.Address}/{pid.Id}", $"Signal is from {msg.Sender.Address}/{msg.Sender.Id} and has total strength (axon connection strength * signal strength) {str}");
                        context.Send(pid, new SignalMessage(str));
                    } else
                    {
                        SendDebugMessage(DebugSeverity.Warning, "Signal", $"Neuron tried to send message using Axon (to Neuron) {axon.ToAddress.RegionPart}/{axon.ToAddress.NeuronPart} that should exist in this region but doesn't.");
                    }
                }
                
                _processed = true;
                break;
            // Broadcasts the TickMessage to all neurons
            case TickMessage msg:
                //Debug.WriteLine($"Region Tick {SelfPID!.Address}/{SelfPID!.Id}");
                //SendDebugMessage(DebugSeverity.Trace, "Tick", "Region received tick", "Broadcasting to all neurons.");
                Broadcast(msg);
                _processed = true;
                break;
        }

        return Task.CompletedTask;
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
    #endregion Behaviors
}
