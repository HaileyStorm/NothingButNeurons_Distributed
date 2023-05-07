using NothingButNeurons.CCSL;
using NothingButNeurons.Shared.Messages;

namespace NothingButNeurons.Shared.DataClasses.Neurons;

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
            (byte)BitMath.DoubleToBits(PreActivationThreshold, 31),
            ActivationFunction,
            (byte)BitMath.DoubleToBits(ActivationParameterA, 63, -3d, 3d));
        NeuronPart2BitField part2 = new(
            (byte)BitMath.DoubleToBits(ActivationParameterB, 63, -3d, 3d),
            (byte)BitMath.DoubleToBits(ActivationThreshold, 15, 0d, 1d),
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
    public static List<NeuronData> ByteArrayToNeuronDataList(byte[] neuronBytes)
    {
        List<NeuronData> neuronDataList = new List<NeuronData>();

        for (int i = 0; i < neuronBytes.Length;)
        {
            int part1Int = BitConverter.ToInt32(neuronBytes.Skip(i).Take(4).ToArray(), 0);
            NeuronPart1BitField part1 = new NeuronPart1BitField(part1Int);
            i += 4;

            int part2Int = BitConverter.ToInt16(neuronBytes.Skip(i).Take(2).Reverse().ToArray(), 0);
            NeuronPart2BitField part2 = new NeuronPart2BitField(part2Int.ReverseBytes());
            i += 2;

            NeuronData neuron = new NeuronData(
                part1.Address,
                part1.AccumulationFunction,
                BitMath.BitsToDouble(part1.PreActivationThreshold, 31),
                part1.ActivationFunction,
                BitMath.BitsToDouble(part1.ActivationParameterA, 63, -3d, 3d),
                BitMath.BitsToDouble(part2.ActivationParameterB, 63, -3d, 3d),
                BitMath.BitsToDouble(part2.ActivationThreshold, 15, 0d, 1d),
                part2.ResetFunction);

            neuronDataList.Add(neuron);
        }

        return neuronDataList;
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