using System.Collections.Specialized;

namespace NothingButNeurons.Brain.Neurons.DataClasses;

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
internal static class NeuronPart2BitVectorSections
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
    public NeuronPart1BitField(byte regionAddress, ushort neuronAddress, byte accumulationFunction, byte preActivationThreshold, byte activationFunction, byte activationParameterA)
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
    public readonly ResetFunction ResetFunction { get { return (ResetFunction)Data[NeuronPart2BitVectorSections.ResetFunction]; } }
    public readonly byte ActivationThreshold { get { return (byte)Data[NeuronPart2BitVectorSections.ActivationThreshold]; } }
    public readonly byte ActivationParameterB { get { return (byte)Data[NeuronPart2BitVectorSections.ActivationParameterB]; } }

    public NeuronPart2BitField(BitVector32 data) { Data = data; }
    public NeuronPart2BitField(int data) { Data = new BitVector32(data.ReverseBytes()); }
    public NeuronPart2BitField(byte activationParameterB, byte activationThreshold, ResetFunction resetFunction)
    {
        BitVector32 data = new();
        data[NeuronPart2BitVectorSections.ResetFunction] = (byte)resetFunction;
        data[NeuronPart2BitVectorSections.ActivationThreshold] = activationThreshold;
        data[NeuronPart2BitVectorSections.ActivationParameterB] = activationParameterB;

        Data = data;
    }
}