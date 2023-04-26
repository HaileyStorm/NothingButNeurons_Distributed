namespace NothingButNeurons.Brain.Regions.Neurons;

/// <summary>
/// Enum representing the different types of reset functions that can be applied to neurons.
/// </summary>
public enum ResetFunction : byte
{
    Zero,
    Hold,
    ClampPotential,
    Clamp1,
    PotentialClampBuffer,
    NegPotentialClampBuffer,
    HundredthsPotentialClampBuffer,
    TenthPotentialClampBuffer,
    HalfPotentialClampBuffer,
    DoublePotentialClampBuffer,
    FiveXPotentialClampBuffer,
    NegHundredthsPotentialClampBuffer,
    NegTenthPotentialClampBuffer,
    NegHalfPotentialClampBuffer,
    NegDoublePotentialClampBuffer,
    NegFiveXPotentialClampBuffer,
    InversePotentialClampBummfer,
    PotentialClamp1,
    NegPotentialClamp1,
    HundredthsPotentialClamp1,
    TenthPotentialClamp1,
    HalfPotentialClamp1,
    DoublePotentialClamp1,
    FiveXPotentialClamp1,
    NegHundredthsPotentialClamp1,
    NegTenthPotentialClamp1,
    NegHalfPotentialClamp1,
    NegDoublePotentialClamp1,
    NegFiveXPotentialClamp1,
    InversePotentialClamp1,
    Potential,
    NegPotential,
    HundredthsPotential,
    TenthPotential,
    HalfPotential,
    DoublePotential,
    FiveXPotential,
    NegHundredthsPotential,
    NegTenthPotential,
    NegHalfPotential,
    NegDoublePotential,
    NegFiveXPotential,
    InversePotential,
    Half,
    Tenth,
    Hundredth,
    Negative,
    NegHalf,
    NegTenth,
    NegHundredth,
    DoubleClamp1,
    FiveXClamp1,
    NegDoubleClamp1,
    NegFiveXClamp1,
    Double,
    FiveX,
    NegDouble,
    NegFiveX,
    DivideAxonCt,
    InverseClamp1,
    Inverse
}

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
                return new NeuronFunctionReturn(0d, 0d);
            case ResetFunction.Hold:
                return new NeuronFunctionReturn(Math.Clamp(buffer, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.ClampPotential:
                return new NeuronFunctionReturn(Math.Clamp(buffer, -1d * Math.Abs(potential), Math.Abs(potential)), 1d);
            case ResetFunction.Clamp1:
                return new NeuronFunctionReturn(Math.Clamp(buffer, -1d, 1d), 1d);
            case ResetFunction.PotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 1d);
            case ResetFunction.NegPotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(-1d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 1d);
            case ResetFunction.HundredthsPotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(0.01d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 1d);
            case ResetFunction.TenthPotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(0.1d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 1d);
            case ResetFunction.HalfPotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(0.5d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 1d);
            case ResetFunction.DoublePotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(2d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 2d);
            case ResetFunction.FiveXPotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(5d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 3d);
            case ResetFunction.NegHundredthsPotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(-0.01d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 1d);
            case ResetFunction.NegTenthPotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(-.1d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 1d);
            case ResetFunction.NegHalfPotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(-.5d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 1d);
            case ResetFunction.NegDoublePotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(-2d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 2d);
            case ResetFunction.NegFiveXPotentialClampBuffer:
                return new NeuronFunctionReturn(Math.Clamp(-5d * potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 3d);
            case ResetFunction.InversePotentialClampBummfer:
                return new NeuronFunctionReturn(Math.Clamp(1d / potential, -1d * Math.Abs(buffer), Math.Abs(buffer)), 1.5d);
            case ResetFunction.PotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(potential, -1d, 1d), 1d);
            case ResetFunction.NegPotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(-1d * potential, -1d, 1d), 1d);
            case ResetFunction.HundredthsPotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(0.01d * potential, -1d, 1d), 1d);
            case ResetFunction.TenthPotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(0.1d * potential, -1d, 1d), 1d);
            case ResetFunction.HalfPotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(0.5d * potential, -1d, 1d), 1d);
            case ResetFunction.DoublePotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(2d * potential, -1d, 1d), 2d);
            case ResetFunction.FiveXPotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(5d * potential, -1d, 1d), 3d);
            case ResetFunction.NegHundredthsPotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(-0.01d * potential, -1d, 1d), 1d);
            case ResetFunction.NegTenthPotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(-0.1d * potential, -1d, 1d), 1d);
            case ResetFunction.NegHalfPotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(-0.5 * potential, -1d, 1d), 1d);
            case ResetFunction.NegDoublePotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(-2d * potential, -1d, 1d), 2d);
            case ResetFunction.NegFiveXPotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(-5d * potential, -1d, 1d), 3d);
            case ResetFunction.InversePotentialClamp1:
                return new NeuronFunctionReturn(Math.Clamp(1d / potential, -1d, 1d), 1.5d);
            case ResetFunction.Potential:
                return new NeuronFunctionReturn(Math.Clamp(potential, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.NegPotential:
                return new NeuronFunctionReturn(Math.Clamp(-1d * potential, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.HundredthsPotential:
                return new NeuronFunctionReturn(Math.Clamp(0.01d * potential, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.TenthPotential:
                return new NeuronFunctionReturn(Math.Clamp(0.1d * potential, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.HalfPotential:
                return new NeuronFunctionReturn(Math.Clamp(0.5d * potential, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.DoublePotential:
                return new NeuronFunctionReturn(Math.Clamp(2d * potential, -1d * activationThreshold, activationThreshold), 2d);
            case ResetFunction.FiveXPotential:
                return new NeuronFunctionReturn(Math.Clamp(5d * potential, -1d * activationThreshold, activationThreshold), 3d);
            case ResetFunction.NegHundredthsPotential:
                return new NeuronFunctionReturn(Math.Clamp(-0.01d * potential, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.NegTenthPotential:
                return new NeuronFunctionReturn(Math.Clamp(-0.1d * potential, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.NegHalfPotential:
                return new NeuronFunctionReturn(Math.Clamp(-0.5d * potential, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.NegDoublePotential:
                return new NeuronFunctionReturn(Math.Clamp(-2d * potential, -1d * activationThreshold, activationThreshold), 2d);
            case ResetFunction.NegFiveXPotential:
                return new NeuronFunctionReturn(Math.Clamp(-5d * potential, -1d * activationThreshold, activationThreshold), 3d);
            case ResetFunction.InversePotential:
                return new NeuronFunctionReturn(Math.Clamp(1d / potential, -1d * activationThreshold, activationThreshold), 1.5d);
            case ResetFunction.Half:
                return new NeuronFunctionReturn(Math.Clamp(0.5d * buffer, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.Tenth:
                return new NeuronFunctionReturn(Math.Clamp(0.1d * buffer, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.Hundredth:
                return new NeuronFunctionReturn(Math.Clamp(0.01d * buffer, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.Negative:
                return new NeuronFunctionReturn(Math.Clamp(-1d * buffer, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.NegHalf:
                return new NeuronFunctionReturn(Math.Clamp(-0.5d * buffer, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.NegTenth:
                return new NeuronFunctionReturn(Math.Clamp(-0.1d * buffer, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.NegHundredth:
                return new NeuronFunctionReturn(Math.Clamp(-0.01d * buffer, -1d * activationThreshold, activationThreshold), 1d);
            case ResetFunction.DoubleClamp1:
                return new NeuronFunctionReturn(Math.Clamp(2d * buffer, -1d, 1d), 2d);
            case ResetFunction.FiveXClamp1:
                return new NeuronFunctionReturn(Math.Clamp(5d * buffer, -1d, 1d), 3d);
            case ResetFunction.NegDoubleClamp1:
                return new NeuronFunctionReturn(Math.Clamp(-2d * buffer, -1d, 1d), 2d);
            case ResetFunction.NegFiveXClamp1:
                return new NeuronFunctionReturn(Math.Clamp(-5d * buffer, -1d, 1d), 3d);
            case ResetFunction.Double:
                return new NeuronFunctionReturn(Math.Clamp(2d * buffer, -1d * activationThreshold, activationThreshold), 2d);
            case ResetFunction.FiveX:
                return new NeuronFunctionReturn(Math.Clamp(5d * buffer, -1d * activationThreshold, activationThreshold), 3d);
            case ResetFunction.NegDouble:
                return new NeuronFunctionReturn(Math.Clamp(-2d * buffer, -1d * activationThreshold, activationThreshold), 2d);
            case ResetFunction.NegFiveX:
                return new NeuronFunctionReturn(Math.Clamp(-5d * buffer, -1d * activationThreshold, activationThreshold), 3d);
            case ResetFunction.DivideAxonCt:
                return new NeuronFunctionReturn(Math.Clamp(buffer / axonCt, -1d * activationThreshold, activationThreshold), 1.5d);
            case ResetFunction.InverseClamp1:
                return new NeuronFunctionReturn(Math.Clamp(-1d / buffer, -1d, 1d), 1.5d);
            case ResetFunction.Inverse:
                return new NeuronFunctionReturn(Math.Clamp(-1d / buffer, -1d * activationThreshold, activationThreshold), 1.5d);
            default:
                throw new NotImplementedException("Unknown ResetFunction.");
        }
    }
}