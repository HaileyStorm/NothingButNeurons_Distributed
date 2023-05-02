using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Brain.Regions.Neurons.DataClasses;

public struct SynapseData
{
    public NeuronAddress FromAddress { get; init; }
    public NeuronAddress ToAddress { get; init; }
    public double Strength { get; init; }

    public SynapseData(NeuronAddress fromAddress, NeuronAddress toAddress, double strength)
    {
        FromAddress = fromAddress;
        ToAddress = toAddress;
        Strength = strength;
    }

    public SynapseBitField ToBitField()
    {
        return new SynapseBitField(FromAddress, ToAddress, (byte)NeuronBase.DoubleToBits(Strength));
    }

    public static SynapseBitField ToBitField(SynapseData synapseData)
    {
        return synapseData.ToBitField();
    }
}

public static class SynapseDataExtensions
{
    public static byte[] ToByteArray(this IEnumerable<SynapseData> synapseData)
    {
        return synapseData.ToList().ConvertAll(SynapseData.ToBitField).ToByteArray();
    }
}