﻿using NothingButNeurons.Shared.Messages;

namespace NothingButNeurons.Shared.DataClasses.Neurons;

// An axon is, basically, a single-ended synapse
public struct Axon
{
    public NeuronAddress ToAddress { get; init; }
    public double Strength { get; init; }

    public Axon(SynapseBitField synapse)
    {
        ToAddress = synapse.ToAddress;
        Strength = BitMath.BitsToDouble(synapse.Strength);
    }

    /// <summary>
    /// Represents an Axon with a target NeuronAddress and a strength value.
    /// </summary>
    public Axon(NeuronAddress toAddress, double strength)
    {
        ToAddress = toAddress;
        Strength = strength;
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
        return new SynapseBitField(fromAddress, ToAddress, (byte)BitMath.DoubleToBits(Strength));
    }
}