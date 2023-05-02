namespace NothingButNeurons.Brain.Neurons.Functions;

/// <summary>
/// Provides extension methods for the ResetFunction enum.
/// </summary>
internal static class ResetFunctionExtensions
{
    /// <summary>
    /// Applies the given reset function to a neuron, returning a <see cref="NeuronFunctionReturn"/> object
    /// containing the new buffer value and the cost.
    /// </summary>
    /// <param name="function">The ResetFunction instance to apply.</param>
    /// <param name="buffer">The neuron's current buffer value.</param>
    /// <param name="potential">The neuron's current potential value.</param>
    /// <param name="activationThreshold">The neuron's current activation threshold value.</param>
    /// <param name="axonCt">The number of axons connected to the neuron.</param>
    /// <returns>A NeuronFunctionReturn object containing the new buffer value and the cost.</returns>
    internal static NeuronFunctionReturn Reset(this ResetFunction function, double buffer, double potential, double activationThreshold, int axonCt)
    {
        // Each case applies a different reset function based on the given ResetFunction value.
        switch (function)
        {
            case ResetFunction.Zero:
                return new NeuronFunctionReturn { Val = 0d, Cost = 0d };
            case ResetFunction.Hold:
                return new NeuronFunctionReturn { Val = Math.Clamp(buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.ClampPotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(buffer, -1d * Math.Abs(potential), Math.Abs(potential)), Cost = 1d };
            case ResetFunction.Clamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(buffer, -1d, 1d), Cost = 1d };
            case ResetFunction.PotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 1d };
            case ResetFunction.NegPotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(-1d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 1d };
            case ResetFunction.HundredthsPotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.01d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 1d };
            case ResetFunction.TenthPotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.1d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 1d };
            case ResetFunction.HalfPotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.5d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 1d };
            case ResetFunction.DoublePotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(2d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 2d };
            case ResetFunction.FiveXpotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(5d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 3d };
            case ResetFunction.NegHundredthsPotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(-0.01d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 1d };
            case ResetFunction.NegTenthPotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(-.1d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 1d };
            case ResetFunction.NegHalfPotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(-.5d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 1d };
            case ResetFunction.NegDoublePotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(-2d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 2d };
            case ResetFunction.NegFiveXpotentialClampBuffer:
                return new NeuronFunctionReturn { Val = Math.Clamp(-5d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 3d };
            case ResetFunction.InversePotentialClampBummfer:
                return new NeuronFunctionReturn { Val = Math.Clamp(1d / potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), Cost = 1.5d };
            case ResetFunction.PotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(potential, -1d, 1d), Cost = 1d };
            case ResetFunction.NegPotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(-1d * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.HundredthsPotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.01d * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.TenthPotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.1d * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.HalfPotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.5d * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.DoublePotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(2d * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.FiveXpotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(5d * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.NegHundredthsPotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(-0.01d * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.NegTenthPotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(-0.1d * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.NegHalfPotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(-0.5 * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.NegDoublePotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(-2d * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.NegFiveXpotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(-5d * potential, -1d, 1d), Cost = 1d };
            case ResetFunction.InversePotentialClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(1d / potential, -1d, 1d), Cost = 1d };
            case ResetFunction.Potential:
                return new NeuronFunctionReturn { Val = Math.Clamp(potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegPotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(-1d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.HundredthsPotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.01d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.TenthPotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.1d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.HalfPotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.5d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.DoublePotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(2d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.FiveXpotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(5d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegHundredthsPotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(-0.01d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegTenthPotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(-0.1d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegHalfPotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(-0.5d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegDoublePotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(-2d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegFiveXpotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(-5d * potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.InversePotential:
                return new NeuronFunctionReturn { Val = Math.Clamp(1d / potential, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.Half:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.5d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.Tenth:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.1d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.Hundredth:
                return new NeuronFunctionReturn { Val = Math.Clamp(0.01d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.Negative:
                return new NeuronFunctionReturn { Val = Math.Clamp(-1d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegHalf:
                return new NeuronFunctionReturn { Val = Math.Clamp(-0.5d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegTenth:
                return new NeuronFunctionReturn { Val = Math.Clamp(-0.1d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegHundredth:
                return new NeuronFunctionReturn { Val = Math.Clamp(-0.01d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.DoubleClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(2d * buffer, -1d, 1d), Cost = 2d };
            case ResetFunction.FiveXclamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(5d * buffer, -1d, 1d), Cost = 3d };
            case ResetFunction.NegDoubleClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(-2d * buffer, -1d, 1d), Cost = 2d };
            case ResetFunction.NegFiveXclamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(-5d * buffer, -1d, 1d), Cost = 3d };
            case ResetFunction.Double:
                return new NeuronFunctionReturn { Val = Math.Clamp(2d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.FiveX:
                return new NeuronFunctionReturn { Val = Math.Clamp(5d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegDouble:
                return new NeuronFunctionReturn { Val = Math.Clamp(-2d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.NegFiveX:
                return new NeuronFunctionReturn { Val = Math.Clamp(-5d * buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.DivideAxonCt:
                return new NeuronFunctionReturn { Val = Math.Clamp(buffer / axonCt, -1d * activationThreshold, activationThreshold), Cost = 1d };
            case ResetFunction.InverseClamp1:
                return new NeuronFunctionReturn { Val = Math.Clamp(-1d / buffer, -1d, 1d), Cost = 1.5d };
            case ResetFunction.Inverse:
                return new NeuronFunctionReturn { Val = Math.Clamp(-1d / buffer, -1d * activationThreshold, activationThreshold), Cost = 1d };
            default:
                throw new NotImplementedException("Unknown ResetFunction.");
        }
    }
}