﻿using System.Collections.Specialized;

namespace NothingButNeurons.Shared.DataClasses.Neurons;

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
    /// Basically distance is (how far from either end of 0-1023, modular, source neuron is + same for destination neuron) * number of regions apart
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