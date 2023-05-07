namespace NothingButNeurons.Shared.DataClasses.Neurons;

public static class BitMath
{
    /// <summary>
    /// Converts Neuron/SynapseBitField stored values (binary, representing integers, e.g. a value between 0 and 15 for a BitVector32.Section taking 4 bits) into doubles (e.g. for Axon strengths, neuron thresholds, activation function parameters).
    /// Values are concentrated toward the mid-range.
    /// Both mid-(binary)-range values are rounded to 0 (if the double range is centered on zero).
    /// </summary>
    /// <param name="bitsVal"></param>
    /// <param name="bitsMax"></param>
    /// <param name="doubleMin"></param>
    /// <param name="doubleMax"></param>
    /// <returns></returns>
    public static double BitsToDouble(uint bitsVal, uint bitsMax = 15, double doubleMin = -1d, double doubleMax = 1d)
    {
        bitsVal = Math.Clamp(bitsVal, 0, bitsMax);
        uint mid = (bitsMax + 1) / 2;
        if ((bitsVal == mid || bitsVal == mid - 1) && Math.Abs(doubleMin) == Math.Abs(doubleMax))
        {
            return 0d;
        }

        double val = bitsVal;
        double max = bitsMax;

        // Look, just trust me. Or as GPT-4 put it:  Apply transformation to concentrate values toward the mid-range.
        double res = Math.Tanh(1d - 2d * val / max);
        res = -1.13188d * Math.Pow(res, 3d) - 0.656518 * res;
        // Clamp the result and scale it to the desired double range.
        return Math.Clamp(doubleMin + 0.5d * (doubleMax - doubleMin) * (res + 1d), doubleMin, doubleMax);
    }

    /// <summary>
    /// Converts doubles (e.g. for Axon strengths, neuron thresholds, activation function parameters) to Neuron/SynapseBitField stored values (binary, representing integers, e.g. a value between 0 and 15 for a BitVector32.Section taking 4 bits).
    /// Values are concentrated toward the mid-range.
    /// Result is rounded away from zero to integer.
    /// </summary>
    /// <param name="doubleVal"></param>
    /// <param name="bitsMax"></param>
    /// <param name="doubleMin"></param>
    /// <param name="doubleMax"></param>
    /// <returns></returns>
    public static uint DoubleToBits(double doubleVal, uint bitsMax = 15, double doubleMin = -1d, double doubleMax = 1d)
    {
        doubleVal = Math.Clamp(doubleVal, doubleMin, doubleMax);

        double max = bitsMax;

        // Remember what I said? Doubly so. Just go with it. Or, per GPT-4: Apply inverse transformation to convert double values to bits.
        double res = (doubleMin + doubleMax - 2d * doubleVal) / (doubleMin - doubleMax);
        double res3 = Math.Cbrt(651.50d * Math.Sqrt(955023750000000000d * Math.Pow(res, 2d) + 35371210793077979d) - 636682500000d * res);
        res = Math.Clamp(0.5d * (0.00017706d * res3 - 4367.9d / res3), -1d, 1d);
        // Round and clamp the result to the desired bit range.
        return (uint)Math.Clamp(Math.Round(0.5d * (max - max * ((Math.Log(1d + res) - Math.Log(1d - res)) / 2d)), MidpointRounding.AwayFromZero), 0, bitsMax);
    }
}
