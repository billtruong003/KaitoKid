using System;
using System.Collections.Generic;
using UnityEditor;

namespace Stratton.Core.Editor
{
    public class TimestampToDateWindow : EditorWindow
    {
        #region Fields

        private long _timestamp;

        #endregion

        #region Public Methods

        [MenuItem("Window/Other/TimestampToDate")]
        public static void ShowWindow()
        {
            GetWindow(typeof(TimestampToDateWindow));
        }

        public static DateTime UnixTimestampToDateTime(long unixTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            long unixTimeStampInTicks = unixTime * TimeSpan.TicksPerMillisecond;
            return new DateTime(unixStart.Ticks + unixTimeStampInTicks, DateTimeKind.Utc);
        }

        public static long DateTimeToUnixTimestamp(DateTime dateTime)
        {
            DateTime unixStart = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            long unixTimeStampInTicks = (dateTime.ToUniversalTime() - unixStart).Ticks;
            return unixTimeStampInTicks / TimeSpan.TicksPerMillisecond;
        }

        #endregion

        #region Private Methods

        void OnGUI()
        {
            _timestamp = EditorGUILayout.LongField("Value(UTC in ms):", _timestamp);
            try
            {
                var dateInUtc = UnixTimestampToDateTime(_timestamp);
                EditorGUILayout.TextField("Date(UTC):", dateInUtc.ToString());
                EditorGUILayout.TextField("Date(Poland + Polish Summer format):",
                    dateInUtc.AddHours(2).ToString("HH:mm:ss.fff dd/MM/yyyy"));
                EditorGUILayout.TextField("Date(Poland + Polish Winter format):",
                    dateInUtc.AddHours(1).ToString("HH:mm:ss.fff dd/MM/yyyy"));
                EditorGUILayout.Space();
                EditorGUILayout.TextField("Current timestamp(UTC):", DateTimeToUnixTimestamp(DateTime.UtcNow).ToString());
                EditorGUILayout.TextField("Current Date(UTC):", DateTime.UtcNow.ToString());
                EditorGUILayout.TextField("Current Date(UTC, ISO):",
                    DateTime.UtcNow.ToString(@"yyyy-MM-dd\THH:mm:ss.fff\Z"));
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox(e.ToString(), MessageType.Error);
            }
        }

        #endregion
    }
}