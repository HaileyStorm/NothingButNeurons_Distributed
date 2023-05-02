using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Brain.Regions.Neurons.DataClasses;

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