using System;
using System.Globalization;
using UnityEngine;

namespace Stratton.Core
{
    [Serializable]
    public struct UnityDateTime //TODO: add interfaces
    {
        public DateTime DateTime
        {
            get
            {
                if (string.IsNullOrEmpty(days)) return DateTime.Now;
                DateTime dt;
                if (DateTime.TryParseExact(days, "u", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal,
                    out dt))
                    return dt;
                return DateTime.TryParse(days, out dt) ? dt : DateTime.Now;
            }
            set { days = value.ToString("u",CultureInfo.InvariantCulture); }
        }

        [SerializeField]
        private string days;

        public UnityDateTime(DateTime dateTime)
        {
            days = dateTime.ToString("u", CultureInfo.InvariantCulture);
        }

        public static implicit operator DateTime(UnityDateTime unityDateTime)
        {
            return unityDateTime.DateTime;
        }

        public static implicit operator UnityDateTime(DateTime dateTime)
        {
            return new UnityDateTime(dateTime);
        }

        public static bool TryParse(string str, out UnityDateTime unityDateTime)
        {
            DateTime dateTime;
            if (DateTime.TryParseExact(str, "u",CultureInfo.InvariantCulture,DateTimeStyles.AdjustToUniversal, out dateTime))
            {
                unityDateTime = dateTime;
                return true;
            }
            else if (DateTime.TryParse(str, out dateTime))
            {
                unityDateTime = dateTime;
                return true;
            }
            else
            {
                unityDateTime = new UnityDateTime();
                return false;
            }
        }

        public static UnityDateTime Parse(string str)
        {
            UnityDateTime unityDateTime;
            if (TryParse(str, out unityDateTime))
            {
                return unityDateTime;
            }
            else
            {
                return System.DateTime.UtcNow;
            }
        }

        public override string ToString()
        {
            return DateTime.ToString();
        }
    }
}
