using System.Numerics;

namespace NothingButNeurons.CCSL;

public static class Binary
{
    static Random Random = new Random();

    public static byte NearestPower(byte val)
    {
        return (byte)NearestPower((uint)val);
    }
    public static ushort NearestPower(ushort val)
    {
        return (ushort)NearestPower((uint)val);
    }
    public static uint NearestPower(uint val)
    {
        return (uint)(val == 1 ? 1 : (1 << (32 - BitOperations.LeadingZeroCount(val - 1))) - 1);
    }
    public static ulong NearestPower(ulong val)
    {
        return (ulong)(val == 1 ? 1 : (1 << (64 - BitOperations.LeadingZeroCount(val - 1))) - 1);
    }

    public static byte[] GetRandomByteArray(int length)
    {
        byte[] result = new byte[length];
        
        Random.NextBytes(result);
        return result;
    }

    public static int ReverseBytes(this int i)
    {
        return BitConverter.ToInt32(BitConverter.GetBytes(i).Reverse().ToArray(), 0);
    }
    /*public static ushort ReverseBytes(this ushort i)
    {
        return BitConverter.ToInt32()
    }*/
}
