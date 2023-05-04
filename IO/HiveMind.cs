using Proto;
using Google.Protobuf;

namespace NothingButNeurons.IO;

/// <summary>
/// HiveMind is responsible for spawning and managing Brain actors and forwarding TickMessages.
/// It is the parent of all Brain actors in NothingButNeurons.
/// </summary>
public class HiveMind : ActorBaseWithBroadcaster
{

    private readonly Behavior _behavior;
    private bool _processed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HiveMind"/> class.
    /// </summary>
    public HiveMind(PID? debugServerPID) : base(debugServerPID)
    {
        _behavior = new Behavior(Spawn);
    }

    #region Behaviors
    /// <summary>
    /// Overrides the ReceiveMessage method from ActorBaseWithBroadcaster, handling messages based on the current behavior.
    /// </summary>
    /// <param name="context">The actor context.</param>
    /// <returns>True if the message was processed, otherwise false.</returns>
    protected override bool ReceiveMessage(IContext context)
    {
        _processed = false;

        _behavior.ReceiveAsync(context).Wait();

        return _processed;
    }

    /// <summary>
    /// Spawn behavior handles messages for spawning Brain actors and activating the HiveMind.
    /// </summary>
    /// <param name="context">The actor context.</param>
    /// <returns>A completed task.</returns>
    private Task Spawn(IContext context)
    {
        switch (context.Message)
        {
            case SpawnBrainMessage msg:
                SpawnBrain(context, msg);
                _processed = true;
                break;
            case SpawnRegionAckMessage:
                // TODO: ? Suppose if we're in Spawn we can do an awaiting count etc, but we would have to instantiate HiveMind with a parameter indicating number of startup Brains, or else send a message with such.
                _processed = true;
                break;
            case ActivateHiveMindMessage:
                _behavior.Become(Active);
                _processed = true;
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Active behavior handles messages when the HiveMind is in its active state.
    /// </summary>
    /// <param name="context">The actor context.</param>
    /// <returns>A completed task.</returns>
    private Task Active(IContext context)
    {
        switch (context.Message)
        {
            case SpawnBrainMessage msg:
                SpawnBrain(context, msg);
                _processed = true;
                break;
            case SpawnRegionAckMessage:
                // TODO: ? Maybe in Spawn; not sure if anything needs to happen when in Active (unless going to build a whole pending Brains list each with its own AwaitingRegionAck count etc so can track without an unfinished Brain count).
                _processed = true;
                break;
            case TickMessage msg:
                //SendDebugMessage(DebugSeverity.Trace, "Tick", "HiveMind received tick", "Broadcasting to all Brains.");
                Broadcast(msg);
                _processed = true;
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Spawns a Brain actor and sends SpawnRegionMessage instances to it.
    /// </summary>
    /// <param name="context">The actor context.</param>
    /// <param name="msg">The SpawnBrainMessage containing neuron and synapse data.</param>
    private void SpawnBrain(IContext context, SpawnBrainMessage msg)
    {
        List<(byte[] neuronData, byte[] synapseData)> regions = FindAllMatchingSections(msg.NeuronData.ToByteArray(), msg.SynapseData.ToByteArray());
        PID pid = context.SpawnPrefix(Props.FromProducer(() => new Brain.Brain(DebugServerPID, regions.Count)), "Brain");
        AddRoutee(pid);
        foreach ((byte[] neuronData, byte[] synapseData) region in regions)
        {
            /*Debug.WriteLine("SpawnBrain parsed region neuronData: ");
            foreach (byte b in region.neuronData)
            {
                string binary = Convert.ToString(b, 2).PadLeft(8, '0');
                Debug.WriteLine(binary);
            }*/
            /*Debug.WriteLine("SpawnBrain parsed region synapseData: ");
            foreach (byte b in region.synapseData)
            {
                string binary = Convert.ToString(b, 2).PadLeft(8, '0');
                Debug.WriteLine(binary);
            }*/
            context.Send(pid, new SpawnRegionMessage { Address = GetLeftMost4Bits(region.neuronData), NeuronData = ByteString.CopyFrom(region.neuronData), SynapseData = ByteString.CopyFrom(region.synapseData) });
        }
        if (context.Sender != null)
            context.Send(context.Sender, new SpawnBrainAckMessage { BrainPID = pid });
    }

    #region Lifecycle methods
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
    #endregion Lifecycle methods
    #endregion Behaviors

    /// <summary>
    /// Finds all neuron and synapse data sections that match a given target.
    /// </summary>
    /// <param name="firstArray">The neuron data array.</param>
    /// <param name="secondArray">The synapse data array.</param>
    /// <returns>A list of tuples containing matching neuron and synapse data.</returns>
    public static List<(byte[] neuronData, byte[] synapseData)> FindAllMatchingSections(byte[] firstArray, byte[] secondArray)
    {
        List<(byte[], byte[])> result;
        Dictionary<int, (byte[] neuronData, byte[] synapseData)> map = new();
        int secondArrayIndex = 0;

        // Iterate through the firstArray and find matching neuron and synapse data based on the left-most 4 bits
        for (int i = 0; i < firstArray.Length; i += 6)
        {
            byte[] neuronData = new byte[6];
            Array.Copy(firstArray, i, neuronData, 0, 6);
            byte target = GetLeftMost4Bits(neuronData);
            if (!map.ContainsKey(target))
            {
                byte[] synapseData = FindMatchingSections(target, secondArray, ref secondArrayIndex);
                map[target] = (neuronData, synapseData);
            }
            else
            {
                // If the target already exists in the map, add the neuronData to the existing tuple
                int length = map[target].neuronData.Length;
                byte[] newNeuronData = new byte[length + 6];
                Array.Copy(map[target].neuronData, 0, newNeuronData, 0, length);
                Array.Copy(neuronData, 0, newNeuronData, length, 6);
                map[target] = (newNeuronData, map[target].synapseData);
            }
        }

        result = map.Values.ToList();

        return result;
    }


    /// <summary>
    /// Finds matching synapse data sections based on a given target.
    /// </summary>
    /// <param name="target">The target value to match.</param>
    /// <param name="secondArray">The synapse data array.</param>
    /// <param name="secondArrayIndex">The index in the secondArray to start searching from.</param>
    /// <returns>An array of matching synapse data.</returns>
    public static byte[] FindMatchingSections(byte target, byte[] secondArray, ref int secondArrayIndex)
    {
        List<byte> result = new();

        // Iterate through the secondArray and find matching synapse data based on the left-most 4 bits
        for (int i = secondArrayIndex; i < secondArray.Length; i += 4)
        {
            if (GetLeftMost4Bits(secondArray.AsSpan(i, 4)) == target)
            {
                result.AddRange(secondArray.Skip(i).Take(4));
            }
            else
            {
                // If the target is not found, update secondArrayIndex and break the loop
                secondArrayIndex = i;
                break;
            }
        }

        return result.ToArray();
    }


    /// <summary>
    /// Gets the left-most 4 bits of the first byte in the input ReadOnlySpan.
    /// </summary>
    /// <param name="input">The input ReadOnlySpan to extract the left-most 4 bits from.</param>
    /// <returns>The left-most 4 bits of the first byte as a byte.</returns>
    private static byte GetLeftMost4Bits(ReadOnlySpan<byte> input)
    {
        return (byte)(input[0] >> 4);
    }
}
