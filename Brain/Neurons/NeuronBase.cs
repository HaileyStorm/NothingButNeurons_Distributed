using Google.Protobuf.WellKnownTypes;
using NothingButNeurons.Brain.Neurons.DataClasses;
using NothingButNeurons.Brain.Neurons.Functions;
using NothingButNeurons.Shared.Messages;
using Proto;

namespace NothingButNeurons.Brain.Neurons;

/// <summary>
/// NeuronBase is an abstract base class representing a neuron in an artificial neural network.
/// </summary>
public abstract class NeuronBase : ActorBase
{
    #region Properties
    /// <summary>
    /// Gets the address of the neuron.
    /// </summary>
    public NeuronAddress Address { get; init; }

    /// <summary>
    /// Gets or sets the array of axons connected to the neuron.
    /// </summary>
    protected Axon[] Axons { get; set; }

    /// <summary>
    /// Gets or sets the number of synapses that are still awaiting to be connected.
    /// </summary>
    private int AwaitingSynapses { get; set; }

    /// <summary>
    /// Gets or sets the signal buffer value of the neuron.
    /// </summary>
    protected double SignalBuffer { get; set; }

    /// <summary>
    /// Gets or sets the accumulation function applied to the neuron.
    /// </summary>
    protected AccumulationFunction AccumulationFunction { get; set; }

    /// <summary>
    /// Gets or sets the pre-activation threshold value for the neuron.
    /// </summary>
    protected double PreActivationThreshold { get; set; }

    /// <summary>
    /// Gets or sets the activation function applied to the neuron.
    /// </summary>
    protected ActivationFunction ActivationFunction { get; set; }

    /// <summary>
    /// Gets or sets the first parameter used in the activation function.
    /// </summary>
    protected double ActivationParameterA { get; set; }

    /// <summary>
    /// Gets or sets the second parameter used in the activation function.
    /// </summary>
    protected double ActivationParameterB { get; set; }

    /// <summary>
    /// Gets or sets the activation threshold value for the neuron.
    /// </summary>
    protected double ActivationThreshold { get; set; }

    /// <summary>
    /// Gets or sets the reset function applied to the neuron.
    /// </summary>
    protected ResetFunction ResetFunction { get; set; }
    #endregion Properties

    // The behavior instance used to manage the current state of the neuron.
    protected readonly Behavior _behavior;
    // A flag indicating whether a message was processed by the neuron.
    private bool _processed;

    /// <summary>
    /// Initializes a new instance of the NeuronBase class with the specified parameters.
    /// </summary>
    /// <param name="address">The address of the neuron.</param>
    /// <param name="synapseCt">The number of synapses connected to the neuron.</param>
    /// <param name="accumulationFunction">The accumulation function applied to the neuron.</param>
    /// <param name="preActivationThreshold">The pre-activation threshold value for the neuron.</param>
    /// <param name="activationFunction">The activation function applied to the neuron.</param>
    /// <param name="activationParameterA">The first parameter used in the activation function.</param>
    /// <param name="activationParameterB">The second parameter used in the activation function.</param>
    /// <param name="activationThreshold">The activation threshold value for the neuron.</param>
    /// <param name="resetFunction">The reset function applied to the neuron.</param>
    protected NeuronBase(PID debugServerPID, int synapseCt, NeuronData neuronData) : base(debugServerPID)
    {
        Address = neuronData.Address;
        AwaitingSynapses = synapseCt;
        Axons = new Axon[synapseCt];
        SignalBuffer = 0d;
        AccumulationFunction = neuronData.AccumulationFunction;
        PreActivationThreshold = neuronData.PreActivationThreshold;
        ActivationFunction = neuronData.ActivationFunction;
        ActivationParameterA = neuronData.ActivationParameterA;
        ActivationParameterB = neuronData.ActivationParameterB;
        ActivationThreshold = neuronData.ActivationThreshold;
        ResetFunction = neuronData.ResetFunction;

        if (AwaitingSynapses > 0)
        {
            _behavior = new Behavior(Spawn);
        }
        else
        {
            _behavior = new Behavior(Disabled);
        }
    }

    #region Behaviours
    /// <summary>
    /// Processes an incoming message based on the current behavior.
    /// </summary>
    /// <param name="context">The actor context.</param>
    /// <returns>True if the message was processed, false otherwise.</returns>
    protected override bool ReceiveMessage(IContext context)
    {
        _processed = false;

        _behavior.ReceiveAsync(context).Wait();

        return _processed;
    }

    /// <summary>
    /// Spawn behavior attempts to create synapses for the neuron.
    ///</summary>
    private Task Spawn(IContext context)
    {
        // Check if all synapses have been spawned.
        void CheckAxons()
        {
            if (AwaitingSynapses == 0)
            {

                // If all synapses have been spawned, switch to the Enabled behavior.
                // TODO: If Axons empty or count 1 and only connection is to self, enter Disabled behavior and tell parent
                SendDebugMessage(DebugSeverity.Trace, "Spawn", "All synapses spawned, Enabling.");
                _behavior.Become(Enabled);
            }
        }

        switch (context.Message)
        {
            // Handle the unstable spawn synapse message and create a new SpawnSynapseReturnMessage with the created Axon.
            case SpawnSynapseMessage msg:
                HandleUnstable(context, msg, (context, msg) => new SpawnSynapseReturnMessage { Axon = new Axon(new SynapseBitField(msg.Data)).ToMsgAxon() });
                _processed = true;
                break;
            // If an exception occurs during synapse spawning, notify the parent and decrement the AwaitingSynapses count.
            case UnstableHandlerException msg:
                context.Send(ParentPID!, new SpawnSynapseFailedMessage { FailedMessage = Any.Pack(msg.FailedMessage) });
                AwaitingSynapses--;
                CheckAxons();
                _processed = true;
                break;
            // Store the created Axon and decrement the AwaitingSynapses count.
            case SpawnSynapseReturnMessage msg:
                Axons[AwaitingSynapses - 1] = new Axon(msg.Axon);
                AwaitingSynapses--;
                CheckAxons();
                // Acknowledge the successful spawning of the synapse.
                context.Send(ParentPID!, new SpawnSynapseAckMessage());
                SendDebugMessage(DebugSeverity.Notice, "Spawn", $"Neuron {SelfPID!.Address}/{SelfPID!.Id} spawned a synapse/axon.", $"Synapse connection to Neuron with address {new Axon(msg.Axon!).ToAddress.RegionPart}/{new Axon(msg.Axon!).ToAddress.NeuronPart} has strength {msg.Axon.Strength}");
                _processed = true;
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disabled behavior prevents the neuron from forwarding any messages to its descendants.
    ///</summary>
    protected Task Disabled(IContext context)
    {
        // Disabled - don't forward any messages to descendants.
        _processed = true;

        switch (context.Message)
        {
            // Enable the neuron and switch to the Enabled behavior.
            case EnableMessage:
                _behavior.Become(Enabled);
                SendDebugMessage(DebugSeverity.Trace, "Neuron Enable", $"{SelfPID!.Address}/{SelfPID.Id} enabled.");
                break;
            // The neuron is already disabled, log the redundant message.
            case DisableMessage:
                SendDebugMessage(DebugSeverity.Debug, "Neuron Disable", $"{SelfPID!.Address}/{SelfPID.Id} received DisableMessage when already disabled.");
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Enabled behavior processes messages related to neuron's activation and signal propagation.
    ///</summary>
    protected Task Enabled(IContext context)
    {
        NeuronFunctionReturn neuronFunctionReturn;
        switch (context.Message)
        {
            // The neuron is already enabled, log the redundant message.
            case EnableMessage:
                _processed = true;
                SendDebugMessage(DebugSeverity.Debug, "Neuron Enable", $"{SelfPID!.Address}/{SelfPID.Id} received EnableMessage when already enabled.");
                break;
            // Disable the neuron and switch to the Disabled behavior.
            case DisableMessage:
                _behavior.Become(Disabled);
                SendDebugMessage(DebugSeverity.Trace, "Neuron Disable", $"{SelfPID!.Address}/{SelfPID.Id} disabled.");
                _processed = true;
                break;
            // Receive signal (Axon activation) and accumulate buffer.
            case SignalMessage signal:
                neuronFunctionReturn = AccumulationFunction.Accumulate(SignalBuffer, signal.Val);
                SignalBuffer = neuronFunctionReturn.Val;
                // TODO: Cost

                // Limit how far down SignalBuffer can go
                double f = PreActivationThreshold < 0 ? -2 : -1;
                SignalBuffer = Math.Max(SignalBuffer, f * Math.Abs(PreActivationThreshold));

                SendDebugMessage(DebugSeverity.Info, "Signal", $"Neuron {SelfPID!.Address}/{SelfPID.Id} processed Signal ({signal.Val})", $"New SignalBuffer: {SignalBuffer}");
                //Debug.WriteLine($"Neuron {SelfPID!.Address}/{SelfPID.Id} processed Signal ({signal.Val}). New SignalBuffer: {SignalBuffer}");
                _processed = true;
                break;
            // Process a tick event, which may cause the neuron to activate and fire signals.
            case TickMessage:
                //SendDebugMessage(DebugSeverity.Trace, "Tick", "Neuron received tick", "Checking SignalBuffer, possibly Activating, firing and Resetting.");
                // TODO: timeout, or a timer of < 1/2 tick time at end of this that must elapse before next tick processed (any before then simply processed=true/return.
                if (SignalBuffer > PreActivationThreshold) // Note this is NOT absolute value, neurons will only fire if buffer is ABOVE threshold (but threshold CAN be negative), negative signals coming in are inhibitory and reduce buffer and delay/prevent firing, excitory increase buffer and hasten it.
                {
                    //SendDebugMessage(DebugSeverity.Trace, "Tick", "SignalBuffer > PreActivationThreshold", "Activating.");
                    // Calculate neuron potential and reset signal buffer.
                    neuronFunctionReturn = ActivationFunction.Activate(SignalBuffer, ActivationParameterA, ActivationParameterB);
                    double potential = neuronFunctionReturn.Val;
                    // TODO: Cost
                    neuronFunctionReturn = ResetFunction.Reset(SignalBuffer, potential, ActivationThreshold, Axons.Length);
                    double reset = neuronFunctionReturn.Val;
                    // TODO: Cost
                    SignalBuffer = reset;
                    SendDebugMessage(DebugSeverity.Info, "Tick", $"Neuron {SelfPID!.Address}/{SelfPID.Id} Activated & Reset", $"Potential = {potential}, and SignalBuffer reset to {reset}. Firing {Axons.Length} axons if > threshold ({Math.Abs(potential)} > {ActivationThreshold}).");
                    //Debug.WriteLine($"Neuron {SelfPID!.Address}/{SelfPID.Id} Activated & Reset", $"Potential = {potential}, and SignalBuffer reset to {reset}. Firing {Axons.Length} axons if > threshold ({Math.Abs(potential)} > {ActivationThreshold}).");
                    // If potential is greater than the activation threshold, fire signals through axons.
                    if (Math.Abs(potential) > ActivationThreshold) // Note this IS absolute value. When a neuron fires, the signal may be negative (and the strength of the connection may be negative and if both, will cancel) ... we want to fire either way (if it's a meaningful signal / above this neuron's threshold)
                    {
                        SendDebugMessage(DebugSeverity.Trace, "Tick", "Potential > ActivationThreshold. Potential > ActivationThreshold ({Math.Abs(potential)} > {ActivationThreshold}). Firing {Axons.Length} axons with potential {potential}. Subsequent debugs have Signal context.");
                        // TODO: Cost, based on distances, using (once DistanceTo has been written): Axons.Sum(a => Address.DistanceTo(a.ToAddress))
                        SignalAxonsMessage signalAxonsMessage = new SignalAxonsMessage { Sender = SelfPID!, Val = potential };
                        signalAxonsMessage.Axons.AddRange(Axons.Select(axon => axon.ToMsgAxon()));
                        context.Send(ParentPID!, signalAxonsMessage);
                    }
                }
                _processed = true;
                break;
        }

        return Task.CompletedTask;
    }
    #endregion Behaviours

    /// <summary>
    /// Converts Neuron/SynapseBitField stored values (binary, representing integers, e.g. a value between 0 and 15 for a BitVector32.Section taking 4 bits) into doubles (e.g. for Axon strengths, neuron thresholds, activation function parameters).
    /// Values are concentrated toward the mid-range.
    /// Both mid-(binary)-range values are rounded to 0 (if the double range is centered on zero).
    /// </summary>
    /// <param name="bitsVal"></param>
    /// <param name="bitsMax"></param>
    /// <param name="doubleMin"></param>
    /// <param name="doubleMax"></param>
    /// <returns></returns>
    public static double BitsToDouble(uint bitsVal, uint bitsMax = 15, double doubleMin = -1d, double doubleMax = 1d)
    {
        bitsVal = Math.Clamp(bitsVal, 0, bitsMax);
        uint mid = (bitsMax + 1) / 2;
        if ((bitsVal == mid || bitsVal == mid - 1) && Math.Abs(doubleMin) == Math.Abs(doubleMax))
        {
            return 0d;
        }

        double val = bitsVal;
        double max = bitsMax;

        // Look, just trust me. Or as GPT-4 put it:  Apply transformation to concentrate values toward the mid-range.
        double res = Math.Tanh(1d - 2d * val / max);
        res = -1.13188d * Math.Pow(res, 3d) - 0.656518 * res;
        // Clamp the result and scale it to the desired double range.
        return Math.Clamp(doubleMin + 0.5d * (doubleMax - doubleMin) * (res + 1d), doubleMin, doubleMax);
    }

    /// <summary>
    /// Converts doubles (e.g. for Axon strengths, neuron thresholds, activation function parameters) to Neuron/SynapseBitField stored values (binary, representing integers, e.g. a value between 0 and 15 for a BitVector32.Section taking 4 bits).
    /// Values are concentrated toward the mid-range.
    /// Result is rounded away from zero to integer.
    /// </summary>
    /// <param name="doubleVal"></param>
    /// <param name="bitsMax"></param>
    /// <param name="doubleMin"></param>
    /// <param name="doubleMax"></param>
    /// <returns></returns>
    public static uint DoubleToBits(double doubleVal, uint bitsMax = 15, double doubleMin = -1d, double doubleMax = 1d)
    {
        doubleVal = Math.Clamp(doubleVal, doubleMin, doubleMax);

        double max = bitsMax;

        // Remember what I said? Doubly so. Just go with it. Or, per GPT-4: Apply inverse transformation to convert double values to bits.
        double res = (doubleMin + doubleMax - 2d * doubleVal) / (doubleMin - doubleMax);
        double res3 = Math.Cbrt(651.50d * Math.Sqrt(955023750000000000d * Math.Pow(res, 2d) + 35371210793077979d) - 636682500000d * res);
        res = Math.Clamp(0.5d * (0.00017706d * res3 - 4367.9d / res3), -1d, 1d);
        // Round and clamp the result to the desired bit range.
        return (uint)Math.Clamp(Math.Round(0.5d * (max - max * ((Math.Log(1d + res) - Math.Log(1d - res)) / 2d)), MidpointRounding.AwayFromZero), 0, bitsMax);
    }
}
