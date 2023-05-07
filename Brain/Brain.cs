global using NothingButNeurons.Shared;
global using NothingButNeurons.Shared.Messages;
global using NothingButNeurons.CCSL;
global using System.Diagnostics;
using NothingButNeurons.Brain.Regions;
using Proto;
using NothingButNeurons.Brain.Neurons.DataClasses;

namespace NothingButNeurons.Brain;

/// <summary>
/// Represents the Brain actor, which manages the communication and coordination of neural network regions.
/// </summary>
public class Brain : ActorBaseWithBroadcaster
{
    /// <summary>
    /// The InputCoordinator PID associated with this Brain actor.
    /// </summary>
    protected PID? InputCoordinator { get; set; }
    /// <summary>
    /// The OutputCoordinator PID associated with this Brain actor.
    /// </summary>
    protected PID? OutputCoordinator { get; set; }
    /// <summary>
    /// The Regions dictionary stores RegionAddress as the key and PID as the value.
    /// </summary>
    protected Dictionary<RegionAddress, PID> Regions { get; set; }
    /// <summary>
    /// The number of regions that the Brain is waiting to spawn.
    /// </summary>
    private int AwaitingRegions { get; set; }
    /// <summary>
    /// The number of neuron acknowledgements the Brain is waiting to receive.
    /// </summary>
    private int AwaitingNeuronAck { get; set; }

    private readonly Behavior _behavior;
    private bool _processed;

    /// <summary>
    /// Initializes a new instance of the Brain class.
    /// </summary>
    /// <param name="regionCount">The number of regions this Brain will manage.</param>
    public Brain(PID debugServerPID, int regionCount) : base(debugServerPID)
    {
        Regions = new();
        AwaitingRegions = regionCount;
        AwaitingNeuronAck = 0;

        _behavior = new Behavior(Spawn);
    }

    #region Behaviors
    /// <summary>
    /// Overrides the ReceiveMessage method from ActorBaseWithBroadcaster, delegates message handling to current behavior (Spawn or Active).
    /// </summary>
    /// <param name="context">The current actor context.</param>
    /// <returns>True if the message was processed, false otherwise.</returns>
    protected override bool ReceiveMessage(IContext context)
    {
        _processed = false;

        _behavior.ReceiveAsync(context).Wait();

        return _processed;
    }

    /// <summary>
    /// The Spawn behavior is responsible for handling the spawning of regions and neurons within the Brain.
    /// </summary>
    /// <param name="context">The current actor context.</param>
    /// <returns>A completed Task.</returns>
    private Task Spawn(IContext context)
    {
        void CheckRegionsAndNeurons()
        {
            if (AwaitingRegions == 0 && AwaitingNeuronAck == 0)
            {
                // If all regions and neurons are spawned, switch to the Active behavior.
                SendDebugMessage(DebugSeverity.Notice, "Spawn", $"Brain {SelfPID!.Address}/{SelfPID!.Id} active.", "All regions and neurons spawned.");
                _behavior.Become(Active);
            }
        }

        switch (context.Message)
        {
            // Handle the message to spawn a new region.
            case SpawnRegionMessage msg:
                int neuronCt = msg.NeuronData.Length / 6;
                PID pid;
                bool isInputRegion = false;
                bool isOutputRegion = false;
                RegionAddress regionAddress = new RegionAddress((byte)msg.Address);
                // Input
                if (new RegionAddress((byte)msg.Address).Address < 6)
                {
                    pid = context.SpawnNamed(Props.FromProducer(() => new InputRegion(DebugServerPID, regionAddress, InputCoordinator!, neuronCt)), regionAddress.Address.ToString());
                    isInputRegion = true;
                }
                // Output
                else if (regionAddress.Address > 12)
                {
                    pid = context.SpawnNamed(Props.FromProducer(() => new OutputRegion(DebugServerPID, new RegionAddress((byte)msg.Address), OutputCoordinator!, neuronCt)), regionAddress.Address.ToString());
                    isOutputRegion = true;
                }
                // Interior
                else
                {
                    pid = context.SpawnNamed(Props.FromProducer(() => new Region(DebugServerPID, regionAddress, neuronCt)), regionAddress.Address.ToString());
                }
                
                Regions[new RegionAddress((byte)msg.Address)] = pid;
                AddRoutee(pid);
                AwaitingNeuronAck += neuronCt;

                // Find all matching sections for the neuron and synapse data (that is, find all the synapses starting at each neuron).
                // This is much more efficient than taking neuronData to a List<NeuronData>, parsing by region, etc. ... thisway we're only converting from binary once, in the neuron/synapse spawn code in Region.
                List<(byte[] neuronData, List<int> synapseData)> neurons = FindAllMatchingSections(msg.NeuronData.ToByteArray(), msg.SynapseData.ToByteArray());
                Span<byte> neuronData;
                List<int> synapseData;
                NeuronPart1BitField part1;
                byte[] part2Data; 
                NeuronPart2BitField part2;
                // Spawn neurons and send the corresponding SpawnNeuronMessage.
                foreach (var neuron in neurons)
                {
                    neuronData = neuron.neuronData.AsSpan();
                    /*Debug.WriteLine("Brain handling SpawnRegion read individual neuronData: ");
                    foreach (byte b in neuronData)
                    {
                        string binary = Convert.ToString(b, 2).PadLeft(8, '0');
                        Debug.WriteLine(binary);
                    }*/
                    synapseData = neuron.synapseData;
                    /*Debug.WriteLine("Brain handling SpawnRegion read individual neuron's synapseData: ");
                    foreach (int i in synapseData)
                    {
                        Debug.WriteLine(Convert.ToString(i, 2));
                    }*/
                    part1 = new NeuronPart1BitField(BitConverter.ToInt32(neuronData[..4]));
                    part2Data = new byte[4];
                    neuronData[4..6].CopyTo(part2Data.AsSpan(2));
                    part2 = new NeuronPart2BitField(BitConverter.ToInt32(part2Data));
                    // Send the SpawnNeuronMessage to the region containing the neuron data and synapse data.
                    var spawnNeuronMessage = new SpawnNeuronMessage
                    {
                        Address = part1.Address.Address.Data,
                        AccumulationFunction = part1.AccumulationFunction,
                        PreActivationThreshold = part1.PreActivationThreshold,
                        ActivationFunction = part1.ActivationFunction,
                        ActivationParameterA = part1.ActivationParameterA,
                        ActivationParameterB = part2.ActivationParameterB,
                        ActivationThreshold = part2.ActivationThreshold,
                        ResetFunction = part2.ResetFunction,
                    };
                    spawnNeuronMessage.Synapses.AddRange(synapseData.ToArray());
                    context.Send(pid, spawnNeuronMessage);
                }

                // Send an acknowledgement message to the parent actor (HiveMind) that the region has been spawned.
                context.Send(ParentPID!, new SpawnRegionAckMessage());
                SendDebugMessage(DebugSeverity.Notice, "Spawn", $"Brain {SelfPID!.Address}/{SelfPID!.Id} spawned {(isInputRegion ? "input" : (isOutputRegion ? "output" : "interior"))} region {pid.Address}/{pid.Id}.", $"Region {pid.Address}/{pid.Id} has address {regionAddress.Address} and contains {neuronCt} neurons.");
                AwaitingRegions--;
                if (Regions.Count == 0) CheckRegionsAndNeurons();

                _processed = true;
                break;
            // Handle a failed neuron spawn message.
            case SpawnNeuronFailedMessage msg:
                // TODO: Handle
                SendDebugMessage(DebugSeverity.Error, "Spawn", "Spawn neuron failed.", msg.FailedMessage.ToString());
                AwaitingNeuronAck--;
                CheckRegionsAndNeurons();
                _processed = true;
                break;
            // Handle a successful neuron spawn acknowledgement message.
            case SpawnNeuronAckMessage:
                AwaitingNeuronAck--;
                CheckRegionsAndNeurons();
                _processed = true;
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// The Active behavior is responsible for handling messages related to the active state of the Brain, such as SignalAxonsMessage and TickMessage.
    /// </summary>
    /// <param name="context">The current actor context.</param>
    /// <returns>A completed Task.</returns>
    private Task Active(IContext context)
    {
        switch (context.Message)
        {
            // Handle the SignalAxonsMessage to forward signals to appropriate regions.
            case SignalAxonsMessage msg:
                SendDebugMessage(DebugSeverity.Trace, "Signal", "Brain received SignalAxonsMessage", $"{msg.Axons.Count} Axons, potential {msg.Val}. Splitting to regions and forwarding.");
                // Split the message into separate messages for each region.
                var result = new Dictionary<byte, List<Axon>>();
                Axon axon;
                foreach (MsgAxon msgAxon in msg.Axons)
                {
                    axon = new Axon(msgAxon);
                    byte regionPart = axon.ToAddress.RegionPart;
                    if (!result.ContainsKey(regionPart))
                    {
                        result[regionPart] = new List<Axon> { axon };
                    }
                    else
                    {
                        result[regionPart].Add(axon);
                    }
                }
                Dictionary<byte, Axon[]> byRegion = result.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());

                // Forward the SignalAxonsMessage to each region.
                foreach (byte region in byRegion.Keys)
                {
                    if (Regions.TryGetValue(new RegionAddress(region), out PID pid))
                    {
                        //SendDebugMessage(DebugSeverity.Trace, "Signal", $"Forwarding SignalAxonsMessage with {byRegion[region].Length} Neurons, potential {msg.Val} to Region {region}");
                        SignalAxonsMessage signalAxonsMessage = new SignalAxonsMessage { Sender = msg.Sender, Val = msg.Val };
                        signalAxonsMessage.Axons.AddRange(byRegion[region].Select(axon => axon.ToMsgAxon()));
                        context.Send(pid, signalAxonsMessage);
                    } else
                    {
                        SendDebugMessage(DebugSeverity.Warning, "Signal", $"Neuron tried to send message to a Neuron within a Region {region} that should exist in this Brain but doesn't.");
                    }
                }
                _processed = true;
                break;
            // Handle the TickMessage to broadcast it to all connected regions.
            case TickMessage msg:
                //SendDebugMessage(DebugSeverity.Trace, "Tick", "Brain received tick", "Broadcasting to all Regions.");
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
        // TODO:
        //InputCoordinator = context.SpawnNamed(Props.FromProducer(() => new InputCoordinator()), "InputCoordinator");
        //OutputCoordinator = context.SpawnNamed(Props.FromProducer(() => new OutputCoordinator()), "OutputCoordinator");
    }

    protected override void ProcessStoppingMessage(IContext context, Stopping msg)
    {
    }

    protected override void ProcessStoppedMessage(IContext context, Stopped msg)
    {
    }
    #endregion Behaviors

    /// <summary>
    /// Finds and returns all matching sections for the given neuron and synapse data arrays.
    /// </summary>
    /// <param name="firstArray">The neuron data array.</param>
    /// <param name="secondArray">The synapse data array.</param>
    /// <returns>A list of tuples containing neuron data and synapse data for each matching section.</returns>
    public static List<(byte[] neuronData, List<int> synapseData)> FindAllMatchingSections(byte[] firstArray, byte[] secondArray)
    {
        List<(byte[] neuronData, List<int> synapseData)> result = new();
        int secondArrayIndex = 0;

        for (int i = 0; i < firstArray.Length; i += 6)
        {
            byte[] neuronData = new byte[6];
            Array.Copy(firstArray, i, neuronData, 0, 6);
            List<int> synapseData = FindMatchingSections(neuronData, secondArray, ref secondArrayIndex);
            result.Add((neuronData, synapseData));
        }

        return result;
    }

    /// <summary>
    /// Finds matching sections for the given neuron data and synapse data array.
    /// </summary>
    /// <param name="neuronData">The neuron data.</param>
    /// <param name="secondArray">The synapse data array.</param>
    /// <param name="secondArrayIndex">The current index in the second array.</param>
    /// <returns>A list of integers representing the matching sections for the given neuron data and synapse data array.</returns>
    public static List<int> FindMatchingSections(byte[] neuronData, byte[] secondArray, ref int secondArrayIndex)
    {
        List<int> result = new();

        int left = secondArrayIndex;
        int right = secondArray.Length / 4 - 1;
        int target = GetLeftMost14Bits(neuronData);

        while (left <= right)
        {
            int mid = (left + right) / 2;
            int midValue = GetLeftMost14Bits(secondArray.AsSpan(mid * 4, 4));

            if (midValue == target)
            {
                // Add all of the matching sections to the result list
                int i = mid;
                while (i >= 0 && GetLeftMost14Bits(secondArray.AsSpan(i * 4, 4)) == target)
                {
                    result.Add(BitConverter.ToInt32(secondArray, i * 4));
                    i--;
                }

                i = mid + 1;
                while (i * 4 < secondArray.Length && GetLeftMost14Bits(secondArray.AsSpan(i * 4, 4)) == target)
                {
                    result.Add(BitConverter.ToInt32(secondArray, i * 4));
                    i++;
                }

                // Update secondArrayIndex for the next search
                secondArrayIndex = i;
                break;
            }
            else if (midValue < target)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the leftmost 14 bits of the input data.
    /// </summary>
    /// <param name="input">The input data as a byte array.</param>
    /// <returns>An integer representing the leftmost 14 bits of the input data.</returns>
    public static int GetLeftMost14Bits(Span<byte> input)
    {
        return BitConverter.ToUInt16(input[..2].ToArray().Reverse().ToArray()) & 0xFFFC;
    }
}
