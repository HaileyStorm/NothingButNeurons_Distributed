using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Brain.Regions.Neurons.DataClasses;

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
public static class SynapseExtensions
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

    public static byte[] ToByteArray(this IEnumerable<SynapseBitField> synapses)
    {
        List<SynapseBitField> synapseList = synapses.ToList();
        synapseList.Sort(new SynapseBitFieldComparer());

        List<int> ints = new List<int>();
        foreach (SynapseBitField synapse in synapseList)
        {
            ints.Add(synapse.Data.Data.ReverseBytes());
        }

        return ints.AsEnumerable().SelectMany(BitConverter.GetBytes).ToArray();
    }

    public class SynapseBitFieldComparer : IComparer<SynapseBitField>
    {
        public int Compare(SynapseBitField x, SynapseBitField y)
        {
            int result = x.FromRegionPart.CompareTo(y.FromRegionPart);
            if (result != 0)
                return result;

            result = x.FromNeuronPart.CompareTo(y.FromNeuronPart);
            if (result != 0)
                return result;

            result = x.ToRegionPart.CompareTo(y.ToRegionPart);
            if (result != 0)
                return result;

            return x.ToNeuronPart.CompareTo(y.ToNeuronPart);
        }
    }
}