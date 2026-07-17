using System;

namespace Stratton.Core
{
    public static class DateTimeExtensions
    {
        #region Public Methods

        public static string ToISOString(this DateTime date, string format = @"yyyy-MM-dd\THH:mm:ss\Z")
        {
            return date.ToString(format);
        }
	
        public static long ToTimestamp(this DateTime dateTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            long unixTimeStampInTicks = (dateTime.ToUniversalTime() - unixStart).Ticks;
            return unixTimeStampInTicks / TimeSpan.TicksPerMillisecond;
        }

        public static DateTime ToDateTime(this long timestamp)
        {
            DateTime dateTime = new DateTime(1970, 1, 1);
            return dateTime.AddSeconds(timestamp);
        }

        #endregion
    }
}