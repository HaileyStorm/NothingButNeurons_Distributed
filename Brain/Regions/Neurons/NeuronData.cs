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
        this.Address = address;
        this.AccumulationFunction = accumulationFunction;
        this.PreActivationThreshold = preActivationThreshold;
        this.ActivationFunction = activationFunction;
        this.ActivationParameterA = activationParameterA;
        this.ActivationParameterB = activationParameterB;
        this.ActivationThreshold = activationThreshold;
        this.ResetFunction = resetFunction;
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
