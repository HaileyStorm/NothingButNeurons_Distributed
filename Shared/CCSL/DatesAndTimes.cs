using System;

namespace NothingButNeurons.CCSL;

public static  class DatesAndTimes
{
    /// <summary>
    /// Converts Unix time to local DateTime.
    /// </summary>
    /// <param name="unixTime">The Unix time in milliseconds.</param>
    /// <returns>The local DateTime representation of the Unix time.</returns>
    public static DateTime UnixTimeToLocalDateTime(long unixTime)
    {
        return DateTimeOffset.FromUnixTimeMilliseconds(unixTime).LocalDateTime;
    }
}
