namespace NothingButNeurons.Brain.Neurons.Functions;

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
            case ActivationFunction.ActNone:
                return new NeuronFunctionReturn { Val = 0d, Cost = 0d };
            case ActivationFunction.Identity:
                return new NeuronFunctionReturn { Val = buffer, Cost = 1d };
            case ActivationFunction.StepUp:
                return new NeuronFunctionReturn { Val = buffer <= 0d ? 0d : 1d, Cost = 1d };
            case ActivationFunction.StepMid:
                return new NeuronFunctionReturn { Val = buffer < 0d ? -1d : buffer == 0 ? 0d : 1d, Cost = 1d };
            case ActivationFunction.StepDown:
                return new NeuronFunctionReturn { Val = buffer < 0d ? -1d : 0d, Cost = 1d };

            case ActivationFunction.Abs:
                return new NeuronFunctionReturn { Val = Math.Abs(buffer), Cost = 1.1d };
            case ActivationFunction.Clamp:
                return new NeuronFunctionReturn { Val = Math.Clamp(buffer, -1d, 1d), Cost = 1.1d };
            case ActivationFunction.ReLu:
                return new NeuronFunctionReturn { Val = Math.Max(0, buffer), Cost = 1.1d };
            case ActivationFunction.NreLu:
                return new NeuronFunctionReturn { Val = Math.Min(buffer, 0), Cost = 1.1d };

            case ActivationFunction.Sin:
                return new NeuronFunctionReturn { Val = Math.Sin(buffer), Cost = 1.2d };

            case ActivationFunction.Tan:
                return new NeuronFunctionReturn { Val = Math.Clamp(Math.Tan(buffer), -1d, 1d), Cost = 1.3d };

            case ActivationFunction.TanH:
                return new NeuronFunctionReturn { Val = Math.Tanh(buffer), Cost = 1.4d };
            case ActivationFunction.Elu:
                return new NeuronFunctionReturn { Val = buffer > 0 ? buffer : parameterA * (Math.Exp(buffer) - 1d), Cost = 1.4d };
            case ActivationFunction.Exp:
                return new NeuronFunctionReturn { Val = Math.Exp(buffer), Cost = 1.4d };

            case ActivationFunction.PreLu:
                return new NeuronFunctionReturn { Val = buffer >= 0 ? buffer : parameterA * buffer, Cost = 1.5d };
            case ActivationFunction.Log:
                return new NeuronFunctionReturn { Val = buffer == 0d ? 0d : Math.Log(buffer), Cost = 1.5d };
            case ActivationFunction.Mult:
                return new NeuronFunctionReturn { Val = buffer * parameterA, Cost = 1.5d };
            case ActivationFunction.Add:
                return new NeuronFunctionReturn { Val = buffer + parameterA, Cost = 1.5d };

            case ActivationFunction.Sig:
                return new NeuronFunctionReturn { Val = 1d / (1d + Math.Exp(-buffer)), Cost = 1.6d };
            case ActivationFunction.SiLu:
                return new NeuronFunctionReturn { Val = buffer / (1d + Math.Exp(-buffer)), Cost = 1.6d };

            case ActivationFunction.Pclamp:
                return new NeuronFunctionReturn { Val = parameterB <= parameterA ? 0d : Math.Clamp(buffer, parameterA, parameterB), Cost = 1.7d };

            case ActivationFunction.ModL:
                return new NeuronFunctionReturn { Val = buffer % parameterA, Cost = 2.0d };
            case ActivationFunction.ModR:
                return new NeuronFunctionReturn { Val = parameterA % buffer, Cost = 2.0d };

            case ActivationFunction.SoftP:
                return new NeuronFunctionReturn { Val = Math.Log(1d + Math.Exp(buffer)), Cost = 2.1d };
            case ActivationFunction.Selu:
                return new NeuronFunctionReturn { Val = parameterB * (buffer >= 0 ? buffer : parameterA * (Math.Exp(buffer) - 1d)), Cost = 2.0d };

            case ActivationFunction.Lin:
                return new NeuronFunctionReturn { Val = parameterA * buffer + parameterB, Cost = 2.2d };

            case ActivationFunction.LogB:
                return new NeuronFunctionReturn { Val = parameterA == 0d ? 0d : Math.Log(buffer, parameterA), Cost = 2.6d };

            case ActivationFunction.Pow:
                return new NeuronFunctionReturn { Val = Math.Pow(buffer, parameterA), Cost = 3.5d };

            case ActivationFunction.Gauss:
                return new NeuronFunctionReturn { Val = Math.Exp(Math.Pow(-buffer, 2)), Cost = 5.3d };

            case ActivationFunction.Quad:
                return new NeuronFunctionReturn { Val = parameterA * Math.Pow(buffer, 2d) + parameterB * buffer, Cost = 9.8d };

            default:
                throw new NotImplementedException("Unknown ActivationFunction");
        }
    }
}