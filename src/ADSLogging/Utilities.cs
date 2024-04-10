using System;
using System.Buffers.Binary;

namespace ADSLogging
{
    /// <summary>
    /// Provides methods to convert time-related data to string representations.
    /// </summary>
    public static class TimeDataConverter
    {
        /// <summary>
        /// Converts the given time data to a string representation with limited milliseconds precision.
        /// </summary>
        /// <param name="data">The time data to convert.</param>
        /// <param name="isTimeOfDay">Specifies whether the time data represents a time of day only.</param>
        /// <returns>A string representation of the time data with limited milliseconds precision.</returns>
        public static string ToLimitedMillisecondsString(ReadOnlyMemory<byte> data, bool isTimeOfDay = false)
        {
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(BinaryPrimitives.ReadUInt32LittleEndian(data.Span));
            return isTimeOfDay ? $"{timeSpan:hh\\:mm\\:ss\\.fff}" : $"{(int)timeSpan.TotalDays:00}:{timeSpan:hh\\:mm\\:ss\\.fff}";
        }

        /// <summary>
        /// Converts the given time data to a string representation including nanoseconds precision.
        /// </summary>
        /// <param name="data">The time data to convert.</param>
        /// <returns>A string representation of the time data including nanoseconds precision.</returns>
        public static string ToNanosecondsString(ReadOnlyMemory<byte> data)
        {
            long nanoseconds = (long)BinaryPrimitives.ReadUInt64LittleEndian(data.Span);
            TimeSpan timeSpan = TimeSpan.FromTicks(nanoseconds / 100);
            long nanosecondsPart = nanoseconds % 10000000;
            return $"{(int)timeSpan.TotalDays:00}:{timeSpan:hh\\:mm\\:ss\\.fff}.{nanosecondsPart:D3}";
        }

        /// <summary>
        /// Converts the given time data to a DateTime object representing a point in time.
        /// </summary>
        /// <param name="data">The time data to convert.</param>
        /// <returns>A DateTime object representing the point in time.</returns>
        public static DateTime ToDateTime(this ReadOnlyMemory<byte> data)
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return unixEpoch.AddSeconds(BinaryPrimitives.ReadUInt32LittleEndian(data.Span));
        }
    }
}
