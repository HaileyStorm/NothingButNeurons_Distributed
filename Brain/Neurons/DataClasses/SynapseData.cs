using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NothingButNeurons.Brain.Neurons;

namespace NothingButNeurons.Brain.Neurons.DataClasses;

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

    public static List<SynapseData> ByteArrayToSynapseDataList(byte[] synapseBytes)
    {
        var synapseDataList = new List<SynapseData>();

        for (int i = 0; i < synapseBytes.Length; i += 4)
        {
            int synapseInt = BitConverter.ToInt32(synapseBytes, i);
            SynapseBitField synapseBitField = new SynapseBitField(synapseInt);

            NeuronAddress fromAddress = synapseBitField.FromAddress;
            NeuronAddress toAddress = synapseBitField.ToAddress;
            double strength = NeuronBase.BitsToDouble(synapseBitField.Strength);

            SynapseData synapseData = new SynapseData(fromAddress, toAddress, strength);
            synapseDataList.Add(synapseData);
        }

        return synapseDataList;
    }

    public class SynapseDataComparer : IComparer<SynapseData>
    {
        public int Compare(SynapseData x, SynapseData y)
        {
            int result = x.FromAddress.RegionPart.CompareTo(y.FromAddress.RegionPart);
            if (result != 0)
                return result;

            result = x.FromAddress.NeuronPart.CompareTo(y.FromAddress.NeuronPart);
            if (result != 0)
                return result;

            result = x.ToAddress.RegionPart.CompareTo(y.ToAddress.RegionPart);
            if (result != 0)
                return result;

            return x.ToAddress.NeuronPart.CompareTo(y.ToAddress.NeuronPart);
        }
    }
}