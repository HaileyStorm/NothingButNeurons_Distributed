namespace NothingButNeurons.Brain.Regions.Neurons;

/// <summary>
/// Enum representing different activation functions to be used in neurons.
/// </summary>
public enum ActivationFunction : byte
{
    None,
    Identity,
    StepUp,
    StepMid,
    StepDown,
    Abs,
    Clamp,
    ReLU,
    NReLU,
    Sin,
    Tan,
    TanH,
    /// <summary>
    /// Uses parameterA
    /// </summary>
    ELU,
    Exp,
    /// <summary>
    /// Uses parameterA
    /// </summary>
    PReLU,
    Log,
    /// <summary>
    /// Uses parameterA
    /// </summary>
    Mult,
    /// <summary>
    /// Uses parameterA
    /// </summary>
    Add,
    Sig,
    SiLU,
    /// <summary>
    /// Uses parameterA and parameterB
    /// </summary>
    PClamp,
    /// <summary>
    /// Uses parameterA
    /// </summary>
    ModL,
    /// <summary>
    /// Uses parameterA
    /// </summary>
    ModR,
    SoftP,
    /// <summary>
    /// Uses parameterA and parameterB, with standard/default values 1.67326 and 1.0507
    /// </summary>
    SELU,
    /// <summary>
    /// Uses parameterA and parameterB
    /// </summary>
    Lin,
    /// <summary>
    /// Uses parameterA
    /// </summary>
    LogB,
    /// <summary>
    /// Uses parameterA
    /// </summary>
    Pow,
    Gauss,
    /// <summary>
    /// Uses parameterA and parameterB
    /// </summary>
    Quad
}

/// <summary>
/// Provides extension methods for the ActivationFunction enum.
/// </summary>
internal static class ActivationFunctionExtensions
{
    /// <summary>
    /// Applies the given activation function to the buffer using the provided parameters.
    /// </summary>
    /// <param name="function">The activation function to apply.</param>
    /// <param name="buffer">The buffer value.</param>
    /// <param name="parameterA">The first activation parameter (used by some activation functions).</param>
    /// <param name="parameterB">The second activation parameter (used by some activation functions).</param>
    /// <returns>A NeuronFunctionReturn containing the result of the activation function and a multiplier.</returns>
    internal static NeuronFunctionReturn Activate(this ActivationFunction function, double buffer, double parameterA, double parameterB)
    {
        switch (function)
        {
            case ActivationFunction.None:
                return new NeuronFunctionReturn(0d, 1d);
            case ActivationFunction.Identity:
                return new NeuronFunctionReturn(buffer, 1d);
            case ActivationFunction.StepUp:
                return new NeuronFunctionReturn(buffer <= 0d ? 0d : 1d, 1d);
            case ActivationFunction.StepMid:
                return new NeuronFunctionReturn(buffer < 0d ? -1d : buffer == 0 ? 0d : 1d, 1d);
            case ActivationFunction.StepDown:
                return new NeuronFunctionReturn(buffer < 0d ? -1d : 0d, 1d);

            case ActivationFunction.Abs:
                return new NeuronFunctionReturn(Math.Abs(buffer), 1.1d);
            case ActivationFunction.Clamp:
                return new NeuronFunctionReturn(Math.Clamp(buffer, -1d, 1d), 1.1d);
            case ActivationFunction.ReLU:
                return new NeuronFunctionReturn(Math.Max(0, buffer), 1.1d);
            case ActivationFunction.NReLU:
                return new NeuronFunctionReturn(Math.Min(buffer, 0), 1.1d);

            case ActivationFunction.Sin:
                return new NeuronFunctionReturn(Math.Sin(buffer), 1.2d);

            case ActivationFunction.Tan:
                return new NeuronFunctionReturn(Math.Clamp(Math.Tan(buffer), -1d, 1d), 1.3d);

            case ActivationFunction.TanH:
                return new NeuronFunctionReturn(Math.Tanh(buffer), 1.4d);
            case ActivationFunction.ELU:
                return new NeuronFunctionReturn(buffer > 0 ? buffer : parameterA * (Math.Exp(buffer) - 1d), 1.4d);
            case ActivationFunction.Exp:
                return new NeuronFunctionReturn(Math.Exp(buffer), 1.4d);

            case ActivationFunction.PReLU:
                return new NeuronFunctionReturn(buffer >= 0 ? buffer : parameterA * buffer, 1.5d);
            case ActivationFunction.Log:
                return new NeuronFunctionReturn(buffer == 0d ? 0d : Math.Log(buffer), 1.5d);
            case ActivationFunction.Mult:
                return new NeuronFunctionReturn(buffer * parameterA, 1.5d);
            case ActivationFunction.Add:
                return new NeuronFunctionReturn(buffer + parameterA, 1.5d);

            case ActivationFunction.Sig:
                return new NeuronFunctionReturn(1d / (1d + Math.Exp(-buffer)), 1.6d);
            case ActivationFunction.SiLU:
                return new NeuronFunctionReturn(buffer / (1d + Math.Exp(-buffer)), 1.6d);

            case ActivationFunction.PClamp:
                return new NeuronFunctionReturn(parameterB <= parameterA ? 0d : Math.Clamp(buffer, parameterA, parameterB), 1.7d);

            case ActivationFunction.ModL:
                return new NeuronFunctionReturn(buffer % parameterA, 2.0d);
            case ActivationFunction.ModR:
                return new NeuronFunctionReturn(parameterA % buffer, 2.0d);

            case ActivationFunction.SoftP:
                return new NeuronFunctionReturn(Math.Log(1d + Math.Exp(buffer)), 2.1d);
            case ActivationFunction.SELU:
                return new NeuronFunctionReturn(parameterB * (buffer >= 0 ? buffer : parameterA * (Math.Exp(buffer) - 1d)), 2.0d);

            case ActivationFunction.Lin:
                return new NeuronFunctionReturn(parameterA * buffer + parameterB, 2.2d);

            case ActivationFunction.LogB:
                return new NeuronFunctionReturn(parameterA == 0d ? 0d : Math.Log(buffer, parameterA), 2.6d);

            case ActivationFunction.Pow:
                return new NeuronFunctionReturn(Math.Pow(buffer, parameterA), 3.5d);

            case ActivationFunction.Gauss:
                return new NeuronFunctionReturn(Math.Exp(Math.Pow(-buffer, 2)), 5.3d);

            case ActivationFunction.Quad:
                return new NeuronFunctionReturn(parameterA * Math.Pow(buffer, 2d) + parameterB * buffer, 9.8d);

            default:
                throw new NotImplementedException("Unknown ActivationFunction");
        }
    }
}