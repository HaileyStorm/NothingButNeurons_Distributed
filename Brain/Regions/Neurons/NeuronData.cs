using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Brain.Regions.Neurons;

public struct NeuronData
{
    public NeuronAddress Address { get; init; }
    public AccumulationFunction AccumulationFunction { get; init; }
    public double PreActivationThreshold { get; init; }

    public ActivationFunction ActivationFunction { get; init; }
    public double ActivationParameterA { get; init; }
    public double ActivationParameterB { get; init; }
    public double ActivationThreshold { get; init; }
    public ResetFunction ResetFunction { get; init; }

    public NeuronData(NeuronAddress address, AccumulationFunction accumulationFunction, double preActivationThreshold, ActivationFunction activationFunction, double activationParameterA, double activationParameterB, double activationThreshold, ResetFunction resetFunction)
    {
        Address = address;
        AccumulationFunction = accumulationFunction;
        PreActivationThreshold = preActivationThreshold;
        ActivationFunction = activationFunction;
        ActivationParameterA = activationParameterA;
        ActivationParameterB = activationParameterB;
        ActivationThreshold = activationThreshold;
        ResetFunction = resetFunction;
    }

    public (NeuronPart1BitField part1, NeuronPart2BitField part2) ToBitFields()
    {
        NeuronPart1BitField part1 = new(
            Address,
            AccumulationFunction,
            (byte)NeuronBase.DoubleToBits(PreActivationThreshold, 31),
            ActivationFunction,
            (byte)NeuronBase.DoubleToBits(ActivationParameterA, 63, -3d, 3d));
        NeuronPart2BitField part2 = new(
            (byte)NeuronBase.DoubleToBits(ActivationParameterB, 63, -3d, 3d),
            (byte)NeuronBase.DoubleToBits(ActivationThreshold, 15, 0d, 1d),
            ResetFunction);

        return (part1, part2);
    }
}

public static class NeuronDataExtensions
{
    public static byte[] ToByteArray(this IEnumerable<NeuronData> neuronData)
    {
        List<NeuronData> neuronList = neuronData.ToList();
        neuronList.Sort(new NeuronDataComparer());

        List<int> neurons = new();

        foreach (NeuronData neuron in neuronList)
        {
            (NeuronPart1BitField part1, NeuronPart2BitField part2) = neuron.ToBitFields();
            neurons.Add(part1.Data.Data);
            neurons.Add(part2.Data.Data);
        }

        byte[] neuronBytes = new byte[neurons.Count * 3];
        int resultIndex = 0;
        byte[] currentIntBytes;
        for (int i = 0; i < neurons.Count; i++)
        {
            int currentInt = neurons[i];
            int bytesToTake = i % 2 == 0 ? 4 : 2;
            if (bytesToTake == 4)
            {
                currentIntBytes = BitConverter.GetBytes(currentInt).Reverse().ToArray();
            }
            else
            {
                currentIntBytes = BitConverter.GetBytes((short)currentInt).Reverse().ToArray();
            }
            Array.Copy(currentIntBytes, 0, neuronBytes, resultIndex, bytesToTake);
            resultIndex += bytesToTake;
        }

        return neuronBytes;
    }

    public class NeuronDataComparer : IComparer<NeuronData>
    {
        public int Compare(NeuronData x, NeuronData y)
        {
            int result = x.Address.RegionPart.CompareTo(y.Address.RegionPart);
            if (result != 0)
                return result;

            return x.Address.NeuronPart.CompareTo(y.Address.NeuronPart);
        }
    }
}