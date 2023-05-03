using NothingButNeurons.Brain.Neurons.DataClasses;
using NothingButNeurons.Shared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NothingButNeurons.Designer;

internal static class RandomBrain
{
    public static List<NeuronData> GenerateRandomNeurons(int numInputRegions, int inputRegionsNumNeuronsMin, int inputRegionsNumNeuronsMax,
                                                        int numInteriorRegions, int interiorRegionsNumNeuronsMin, int interiorRegionsNumNeuronsMax,
                                                        int numOutputRegions, int outputRegionsNumNeuronsMin, int outputRegionsNumNeuronsMax,
                                                        string accumulationFunction, double preActivationThresholdMin, double preActivationThresholdMax,
                                                        string activationFunction, double activationParameterAMin, double activationParameterAMax,
                                                        double activationParameterBMin, double activationParameterBMax,
                                                        double activationThresholdMin, double activationThresholdMax, string resetFunction)
    {
        // Prepare the available options for "common" functions
        var commonAccumulationFunctions = new[] { AccumulationFunction.Sum, AccumulationFunction.Product };
        var commonActivationFunctions = new[] { ActivationFunction.TanH, ActivationFunction.Identity, ActivationFunction.SiLu, ActivationFunction.Gauss, ActivationFunction.Clamp, ActivationFunction.SoftP, ActivationFunction.ReLu };
        var commonResetFunctions = new[] { ResetFunction.Zero, ResetFunction.Hold, ResetFunction.Clamp1, ResetFunction.Half, ResetFunction.Inverse };

        var random = new Random();
        var neurons = new List<NeuronData>();

        int[] inputRegionIndices = Enumerable.Range(0, numInputRegions).ToArray();
        int[] interiorRegionIndices = Enumerable.Range(6, numInteriorRegions).ToArray();
        int[] outputRegionIndices = Enumerable.Range(13, numOutputRegions).ToArray();

        foreach (var regionIndex in inputRegionIndices.Concat(interiorRegionIndices).Concat(outputRegionIndices))
        {
            int numNeuronsInRegion;
            if (inputRegionIndices.Contains(regionIndex))
                numNeuronsInRegion = random.Next(inputRegionsNumNeuronsMin, inputRegionsNumNeuronsMax + 1);
            else if (interiorRegionIndices.Contains(regionIndex))
                numNeuronsInRegion = random.Next(interiorRegionsNumNeuronsMin, interiorRegionsNumNeuronsMax + 1);
            else
                numNeuronsInRegion = random.Next(outputRegionsNumNeuronsMin, outputRegionsNumNeuronsMax + 1);

            var neuronIndices = new HashSet<int>();
            while (neuronIndices.Count < numNeuronsInRegion)
                neuronIndices.Add(random.Next(0, 1024));

            foreach (var neuronIndex in neuronIndices)
            {
                var neuronAddress = new NeuronAddress((byte)regionIndex, (ushort)neuronIndex);
                var preActivationThreshold = random.NextDouble() * (preActivationThresholdMax - preActivationThresholdMin) + preActivationThresholdMin;
                var activationParameterA = random.NextDouble() * (activationParameterAMax - activationParameterAMin) + activationParameterAMin;
                var activationParameterB = random.NextDouble() * (activationParameterBMax - activationParameterBMin) + activationParameterBMin;
                var activationThreshold = random.NextDouble() * (activationThresholdMax - activationThresholdMin) + activationThresholdMin;

                neurons.Add(new NeuronData(
                address: neuronAddress,
                    accumulationFunction: GetAccumulationFunction(accumulationFunction, commonAccumulationFunctions, random),
                    preActivationThreshold: preActivationThreshold,
                    activationFunction: GetActivationFunction(activationFunction, commonActivationFunctions, random),
                    activationParameterA: activationParameterA,
                    activationParameterB: activationParameterB,
                    activationThreshold: activationThreshold,
                    resetFunction: GetResetFunction(resetFunction, commonResetFunctions, random)));
            }
        }

        return neurons;
    }

    public static List<SynapseData> GenerateRandomSynapses(List<NeuronData> neurons, int inputRegionsNumSynapsesPerNeuronMin, int inputRegionsNumSynapsesPerNeuronMax, double inputRegionsSynapseStrengthMin, double inputRegionsSynapseStrengthMax, int interiorRegionsNumSynapsesPerNeuronMin, int interiorRegionsNumSynapsesPerNeuronMax, double interiorRegionsSynapseStrengthMin, double interiorRegionsSynapseStrengthMax)
    {
        List<SynapseData> synapses = new();
        Random random = new();
        int firstInteriorNeuronIndex = neurons.FindIndex(neuron => neuron.Address.RegionPart >= 6);

        for (int i = 0; i < neurons.Count; i++)
        {
            NeuronData fromNeuron = neurons[i];

            // Stop when reaching the first Output neuron
            if (fromNeuron.Address.RegionPart >= 13)
            {
                break;
            }

            int numSynapses;
            double synapseStrengthMin, synapseStrengthMax;
            if (fromNeuron.Address.RegionPart < 6) // Input neuron
            {
                numSynapses = random.Next(inputRegionsNumSynapsesPerNeuronMin, inputRegionsNumSynapsesPerNeuronMax + 1);
                synapseStrengthMin = inputRegionsSynapseStrengthMin;
                synapseStrengthMax = inputRegionsSynapseStrengthMax;
            }
            else // Interior neuron
            {
                numSynapses = random.Next(interiorRegionsNumSynapsesPerNeuronMin, interiorRegionsNumSynapsesPerNeuronMax + 1);
                synapseStrengthMin = interiorRegionsSynapseStrengthMin;
                synapseStrengthMax = interiorRegionsSynapseStrengthMax;
            }

            HashSet<int> usedToNeuronIndices = new HashSet<int>();
            for (int j = 0; j < numSynapses; j++)
            {
                int toNeuronIndex;
                do
                {
                    toNeuronIndex = random.Next(firstInteriorNeuronIndex, neurons.Count);
                } while (usedToNeuronIndices.Contains(toNeuronIndex));

                usedToNeuronIndices.Add(toNeuronIndex);
                NeuronData toNeuron = neurons[toNeuronIndex];

                SynapseData synapse = new SynapseData(
                    fromAddress: fromNeuron.Address,
                    toAddress: toNeuron.Address,
                    strength: random.NextDouble() * (synapseStrengthMax - synapseStrengthMin) + synapseStrengthMin
                );

                synapses.Add(synapse);
            }
        }

        return synapses;
    }

    private static AccumulationFunction GetAccumulationFunction(string accumulationFunction, AccumulationFunction[] commonAccumulationFunctions, Random random)
    {
        return accumulationFunction switch
        {
            "Random - All" => (AccumulationFunction)random.Next(1, Enum.GetNames(typeof(AccumulationFunction)).Length),
            "Random - Common" => commonAccumulationFunctions[random.Next(commonAccumulationFunctions.Length)],
            _ => Enum.TryParse(accumulationFunction, out AccumulationFunction parsedAccumulationFunction) ? parsedAccumulationFunction : AccumulationFunction.Sum
        };
    }

    private static ActivationFunction GetActivationFunction(string activationFunction, ActivationFunction[] commonActivationFunctions, Random random)
    {
        return activationFunction switch
        {
            "Random - All" => (ActivationFunction)random.Next(1, Enum.GetNames(typeof(ActivationFunction)).Length),
            "Random - Common" => commonActivationFunctions[random.Next(commonActivationFunctions.Length)],
            _ => Enum.TryParse(activationFunction, out ActivationFunction parsedActivationFunction) ? parsedActivationFunction : ActivationFunction.TanH
        };
    }

    private static ResetFunction GetResetFunction(string resetFunction, ResetFunction[] commonResetFunctions, Random random)
    {
        return resetFunction switch
        {
            "Random - All" => (ResetFunction)random.Next(Enum.GetNames(typeof(ResetFunction)).Length),
            "Random - Common" => commonResetFunctions[random.Next(commonResetFunctions.Length)],
            _ => Enum.TryParse(resetFunction, out ResetFunction parsedResetFunction) ? parsedResetFunction : ResetFunction.Zero
        };
    }
}
