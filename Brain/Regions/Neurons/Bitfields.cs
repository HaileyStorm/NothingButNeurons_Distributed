using System.Collections.Specialized;

namespace NothingButNeurons.Brain.Regions.Neurons;


// Internal class containing sections for NeuronPart1BitField's BitVector32
internal static class NeuronPart1BitVectorSections
{
    // Sections for ActivationParameterA, ActivationFunction, PreActivationThreshold, AccumulationFunction, Address, NeuronAddress, and RegionAddress
    public static BitVector32.Section ActivationParameterA = BitVector32.CreateSection(63);
    public static BitVector32.Section ActivationFunction = BitVector32.CreateSection(31, ActivationParameterA);
    public static BitVector32.Section PreActivationThreshold = BitVector32.CreateSection(31, ActivationFunction);
    public static BitVector32.Section AccumulationFunction = BitVector32.CreateSection(3, PreActivationThreshold);
    // Full address
    public static BitVector32.Section Address = BitVector32.CreateSection(16383, AccumulationFunction);
    // Each part
    public static BitVector32.Section NeuronAddress = BitVector32.CreateSection(1023, AccumulationFunction);
    public static BitVector32.Section RegionAddress = BitVector32.CreateSection(15, NeuronAddress);
}

// Internal class containing sections for NeuronPart2BitField's BitVector32
internal static class NeuronPart2BitVectorSetions
{
    // Sections for ResetFunction, ActivationThreshold, and ActivationParameterB
    // Note: Only using 16 bits.
    public static BitVector32.Section ResetFunction = BitVector32.CreateSection(63);
    public static BitVector32.Section ActivationThreshold = BitVector32.CreateSection(15, ResetFunction);
    public static BitVector32.Section ActivationParameterB = BitVector32.CreateSection(63, ActivationThreshold);
}

/// <summary>
/// Represents the first part of the Neuron data bitfield.
/// </summary>
public readonly struct NeuronPart1BitField
{
    // Properties and constructors for NeuronPart1BitField

    public BitVector32 Data { get; init; }
    public readonly byte ActivationParameterA { get { return (byte)Data[NeuronPart1BitVectorSections.ActivationParameterA]; } }
    public readonly ActivationFunction ActivationFunction { get { return (ActivationFunction)Data[NeuronPart1BitVectorSections.ActivationFunction]; } }
    public readonly byte PreActivationThreshold { get { return (byte)Data[NeuronPart1BitVectorSections.PreActivationThreshold]; } }
    public readonly AccumulationFunction AccumulationFunction { get { return (AccumulationFunction)Data[NeuronPart1BitVectorSections.AccumulationFunction]; } }
    public readonly NeuronAddress Address { get { return new NeuronAddress(Data[NeuronPart1BitVectorSections.Address]); } }
    public readonly ushort NeuronAddress { get { return (ushort)Data[NeuronPart1BitVectorSections.NeuronAddress]; } }
    public readonly byte RegionAddress { get { return (byte)Data[NeuronPart1BitVectorSections.RegionAddress]; } }


    public NeuronPart1BitField(BitVector32 data) { Data = data; }
    public NeuronPart1BitField(int data) { Data = new BitVector32(data.ReverseBytes()); }
    public NeuronPart1BitField(NeuronAddress address, byte accumulationFunction, byte preActivationThreshold, byte activationFunction, byte activationParameterA)
    {
        BitVector32 data = new();
        data[NeuronPart1BitVectorSections.ActivationParameterA] = activationParameterA;
        data[NeuronPart1BitVectorSections.ActivationFunction] = activationFunction;
        data[NeuronPart1BitVectorSections.PreActivationThreshold] = preActivationThreshold;
        data[NeuronPart1BitVectorSections.AccumulationFunction] = accumulationFunction;
        data[NeuronPart1BitVectorSections.Address] = address.Address.Data;

        Data = data;
    }
    public NeuronPart1BitField(byte regionAddress,  ushort neuronAddress, byte accumulationFunction, byte preActivationThreshold, byte activationFunction, byte activationParameterA)
    {
        BitVector32 data = new();
        data[NeuronPart1BitVectorSections.ActivationParameterA] = activationParameterA;
        data[NeuronPart1BitVectorSections.ActivationFunction] = activationFunction;
        data[NeuronPart1BitVectorSections.PreActivationThreshold] = preActivationThreshold;
        data[NeuronPart1BitVectorSections.AccumulationFunction] = accumulationFunction;
        data[NeuronPart1BitVectorSections.NeuronAddress] = neuronAddress;
        data[NeuronPart1BitVectorSections.RegionAddress] = regionAddress;

        Data = data;
    }
    public NeuronPart1BitField(NeuronAddress address, AccumulationFunction accumulationFunction, byte preActivationThreshold, ActivationFunction activationFunction, byte activationParameterA)
    {
        BitVector32 data = new();
        data[NeuronPart1BitVectorSections.ActivationParameterA] = activationParameterA;
        data[NeuronPart1BitVectorSections.ActivationFunction] = (byte)activationFunction;
        data[NeuronPart1BitVectorSections.PreActivationThreshold] = preActivationThreshold;
        data[NeuronPart1BitVectorSections.AccumulationFunction] = (byte)accumulationFunction;
        data[NeuronPart1BitVectorSections.Address] = address.Address.Data;

        Data = data;
    }
    public NeuronPart1BitField(byte regionAddress, ushort neuronAddress, AccumulationFunction accumulationFunction, byte preActivationThreshold, ActivationFunction activationFunction, byte activationParameterA)
    {
        BitVector32 data = new();
        data[NeuronPart1BitVectorSections.ActivationParameterA] = activationParameterA;
        data[NeuronPart1BitVectorSections.ActivationFunction] = (byte)activationFunction;
        data[NeuronPart1BitVectorSections.PreActivationThreshold] = preActivationThreshold;
        data[NeuronPart1BitVectorSections.AccumulationFunction] = (byte)accumulationFunction;
        data[NeuronPart1BitVectorSections.NeuronAddress] = neuronAddress;
        data[NeuronPart1BitVectorSections.RegionAddress] = regionAddress;

        Data = data;
    }
}

/// <summary>
/// Represents the second part of the Neuron data bitfield.
/// </summary>
public readonly struct NeuronPart2BitField
{
    // Properties and constructors for NeuronPart2BitField

    public BitVector32 Data { get; init; }
    public readonly ResetFunction ResetFunction { get { return (ResetFunction)Data[NeuronPart2BitVectorSetions.ResetFunction]; } }
    public readonly byte ActivationThreshold { get { return (byte)Data[NeuronPart2BitVectorSetions.ActivationThreshold]; } }
    public readonly byte ActivationParameterB { get { return (byte)Data[NeuronPart2BitVectorSetions.ActivationParameterB]; } }

    public NeuronPart2BitField(BitVector32 data) { Data = data; }
    public NeuronPart2BitField(int data) { Data = new BitVector32(data.ReverseBytes()); }
    public NeuronPart2BitField(byte activationParameterB, byte activationThreshold, ResetFunction resetFunction)
    {
        BitVector32 data = new();
        data[NeuronPart2BitVectorSetions.ResetFunction] = (byte)resetFunction;
        data[NeuronPart2BitVectorSetions.ActivationThreshold] = activationThreshold;
        data[NeuronPart2BitVectorSetions.ActivationParameterB] = (byte)activationParameterB;

        Data = data;
    }
}

// Internal class containing sections for SynapseBitField's BitVector32
internal static class SynapseBitVectorSections
{
    // Sections for Strength, ToAddress, FromAddress, ToNeuronPart, ToRegionPart, FromNeuronPart, FromRegionPart, NeuronPart, and RegionPart
    // Synapses
    public static BitVector32.Section Strength = BitVector32.CreateSection(15);
    // Full to/from addresses
    public static BitVector32.Section ToAddress = BitVector32.CreateSection(16383, Strength);
    public static BitVector32.Section FromAddress = BitVector32.CreateSection(16383, ToAddress);
    // Each part
    public static BitVector32.Section ToNeuronPart = BitVector32.CreateSection(1023, Strength);
    public static BitVector32.Section ToRegionPart = BitVector32.CreateSection(15, ToNeuronPart);
    public static BitVector32.Section FromNeuronPart = BitVector32.CreateSection(1023, ToRegionPart);
    public static BitVector32.Section FromRegionPart = BitVector32.CreateSection(15, FromNeuronPart);


    // NeuronBase address
    public static BitVector32.Section NeuronPart = BitVector32.CreateSection(1023);
    public static BitVector32.Section RegionPart = BitVector32.CreateSection(15, NeuronPart);
}

/// <summary>
/// Represents a Synapse data bitfield.
/// </summary>
public readonly struct SynapseBitField
{
    // Properties and constructors for SynapseBitField

    public BitVector32 Data { get; init; }
    public readonly byte Strength { get { return (byte)Data[SynapseBitVectorSections.Strength]; } }
    public readonly NeuronAddress ToAddress { get { return new NeuronAddress(Data[SynapseBitVectorSections.ToAddress]); } }
    public readonly NeuronAddress FromAddress { get { return new NeuronAddress(Data[SynapseBitVectorSections.FromAddress]); } }
    public readonly ushort ToNeuronPart { get { return (ushort)Data[SynapseBitVectorSections.ToNeuronPart]; } }
    public readonly byte ToRegionPart { get { return (byte)Data[SynapseBitVectorSections.ToRegionPart]; } }
    public readonly ushort FromNeuronPart { get { return (ushort)Data[SynapseBitVectorSections.FromNeuronPart]; } }
    public readonly byte FromRegionPart { get { return (byte)Data[SynapseBitVectorSections.FromRegionPart]; } }

    public SynapseBitField(BitVector32 data) { Data = data; }
    public SynapseBitField(int data) { Data = new BitVector32(data.ReverseBytes()); }

    public SynapseBitField(NeuronAddress fromAddress, NeuronAddress toAddress, byte strength)
    {
        BitVector32 data = new();
        data[SynapseBitVectorSections.Strength] = strength;
        data[SynapseBitVectorSections.ToAddress] = toAddress.Address.Data;
        data[SynapseBitVectorSections.FromAddress] = fromAddress.Address.Data;

        Data = data;
    }
    public SynapseBitField(byte fromRegion, ushort fromNeuron, byte toRegion, ushort toNeuron, byte strength)
    {
        BitVector32 data = new();
        data[SynapseBitVectorSections.Strength] = strength;
        data[SynapseBitVectorSections.ToNeuronPart] = toNeuron;
        data[SynapseBitVectorSections.ToRegionPart] = toRegion;
        data[SynapseBitVectorSections.FromNeuronPart] = fromNeuron;
        data[SynapseBitVectorSections.FromRegionPart] = fromRegion;

        Data = data;
    }
}

/// <summary>
/// Provides extension methods for SynapseBitField.
/// </summary>
internal static class SynapseExtensions
{
    /// <summary>
    /// Converts a collection of SynapseBitField objects into an array of Axon objects.
    /// </summary>
    /// <param name="synapses">The collection of SynapseBitField objects.</param>
    /// <returns>An array of Axon objects.</returns>
    internal static Axon[] ToAxons(this IEnumerable<SynapseBitField> synapses)
    {
        return synapses.Select(s => new Axon(s)).ToArray();
    }
}

/// <summary>
/// Represents a NeuronAddress with a neuron part and a region part.
/// </summary>
public struct NeuronAddress
{
    // Properties and constructors for NeuronAddress

    public BitVector32 Address { get; init; }
    public readonly ushort NeuronPart { get { return (ushort)Address[SynapseBitVectorSections.NeuronPart]; } }
    public readonly byte RegionPart { get { return (byte)Address[SynapseBitVectorSections.RegionPart]; } }

    public NeuronAddress(BitVector32 address) { Address = address; }
    public NeuronAddress(int address) { Address = new BitVector32(address); }

    public NeuronAddress(byte regionPart, ushort neuronPart)
    {
        BitVector32 address = new();
        address[SynapseBitVectorSections.NeuronPart] = neuronPart;
        address[SynapseBitVectorSections.RegionPart] = regionPart;

        Address = address;
    }

    /// <summary>
    /// Calculates the distance between this NeuronAddress and another NeuronAddress.
    /// </summary>
    /// <param name="other">The other NeuronAddress.</param>
    /// <returns>The distance between the NeuronAddresses.</returns>
    public double DistanceTo(NeuronAddress other)
    {
        // TODO: Calculate distance
        return 0;
    }
}

/// <summary>
/// Represents a RegionAddress with a single byte address.
/// </summary>
public struct RegionAddress
{
    // Properties and constructors for RegionAddress

    public byte Address { get; init; }

    public RegionAddress(NeuronAddress address) { Address = address.RegionPart; }
    public RegionAddress(byte address) { Address = address; }
}

public struct Axon
{
    public NeuronAddress ToAddress { get; init; }
    public double Strength { get; init; }

    public Axon(SynapseBitField synapse)
    {
        ToAddress = synapse.ToAddress;
        Strength = NeuronBase.BitsToDouble(synapse.Strength);
    }

    /// <summary>
    /// Represents an Axon with a target NeuronAddress and a strength value.
    /// </summary>
    public Axon(NeuronAddress toAddress, double strength)
    {
        ToAddress = toAddress;
        Strength = strength;
    }
    public Axon(NeuronAddress toAddress, byte strength, bool excitory)
    {
        ToAddress = toAddress;
        Strength = strength / 7d * (excitory ? 1d : -1d);
    }

    public Axon(MsgAxon msgAxon)
    {
        ToAddress = new NeuronAddress(msgAxon.ToAddress);
        Strength = msgAxon.Strength;
    }

    public MsgAxon ToMsgAxon()
    {
        return new MsgAxon
        {
            ToAddress = ToAddress.Address.Data,
            Strength = Strength
        };
    }

    public SynapseBitField ToSynapseBitField(NeuronAddress fromAddress)
    {
        return new SynapseBitField(fromAddress, ToAddress, (byte)NeuronBase.DoubleToBits(Strength));
    }
}