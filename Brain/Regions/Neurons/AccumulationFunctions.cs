namespace NothingButNeurons.Brain.Regions.Neurons;

/// <summary>
/// Provides extension methods for the AccumulationFunction enum.
/// </summary>
internal static class AccumulationFunctionExtensions
{
    /// <summary>
    /// Applies the given accumulation function to the buffer and value.
    /// </summary>
    /// <param name="function">The accumulation function to apply.</param>
    /// <param name="buffer">The buffer value.</param>
    /// <param name="val">The value to be accumulated.</param>
    /// <returns>A NeuronFunctionReturn containing the updated buffer and a multiplier.</returns>
    internal static NeuronFunctionReturn Accumulate(this AccumulationFunction function, double buffer, double val)
    {
        switch (function)
        {
            case AccumulationFunction.AccumNone:
                return new NeuronFunctionReturn { Val = buffer, Cost = 1d };
            case AccumulationFunction.Sum:
                return new NeuronFunctionReturn { Val = buffer + val, Cost = 1.5d };
            case AccumulationFunction.Product:
                return new NeuronFunctionReturn{ Val = buffer * val, Cost = 1.5d };
            default:
                throw new NotImplementedException("Unknown AccumulationFunction.");
        }
    }
}