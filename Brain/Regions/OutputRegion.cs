using NothingButNeurons.Brain.Regions.Neurons;
using Proto;

namespace NothingButNeurons.Brain.Regions;

/// <summary>
/// Represents an output region in the neural network.
/// </summary>
internal class OutputRegion : Region
{
    // Initializes the OutputRegion with a given address, output coordinator PID, and neuron count.
    // Sets IsOutputRegion to true and IsInteriorRegion to false.
    public OutputRegion(PID debugServerPID, RegionAddress address, PID outputCoordinator, int neuronCt) : base(debugServerPID, address, neuronCt)
    {
        OutputCoordinator = outputCoordinator;
        IsOutputRegion = true;
        IsInteriorRegion = false;
    }
}
