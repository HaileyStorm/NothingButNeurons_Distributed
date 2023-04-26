using NothingButNeurons.Brain.Regions.Neurons;
using Proto;

namespace NothingButNeurons.Brain.Regions;

/// <summary>
/// Represents an input region in the neural network.
/// </summary>
internal class InputRegion : Region
{
    // Initializes the InputRegion with a given address, input coordinator PID, and neuron count.
    // Sets IsInputRegion to true and IsInteriorRegion to false.
    public InputRegion(RegionAddress address, PID inputCoordinator, int neuronCt) : base(address, neuronCt)
    {
        InputCoordinator = inputCoordinator;
        IsInputRegion = true;
        IsInteriorRegion = false;
    }
}
