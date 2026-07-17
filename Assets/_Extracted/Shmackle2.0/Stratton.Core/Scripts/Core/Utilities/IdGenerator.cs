using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Random = System.Random;


namespace Stratton.Core
{
    public class IdGenerator
    {
        private static readonly Random random = new Random(Guid.NewGuid().GetHashCode());

        private const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

#if UNITY_EDITOR
        private static string _prefsKey { get { return "device-id" + System.IO.Path.GetDirectoryName(Application.dataPath); } }
#else
    private const string _prefsKey = "device-id"; //storecustom device-id
#endif

        private static string GetPlatformSymbol(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.WindowsPlayer:
                    return "standalone";
                case RuntimePlatform.WebGLPlayer:
                    return "web";
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.WindowsEditor:
                    return "editor";
                case RuntimePlatform.IPhonePlayer:
                    return "ios";
                case RuntimePlatform.Android:
                    return "android";
                case RuntimePlatform.WSAPlayerARM:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerX86:
                    return "wsa";
                case RuntimePlatform.tvOS:
                    return "tvOS";
                default:
                    throw new ArgumentOutOfRangeException("platform");
            }
        }

        public static int Rand(int max)
        {
            return random.Next(max);
        }

        private static string GetTimestamp()
        {
            StringBuilder result = new StringBuilder();
            string currentDateTime = DateTime.UtcNow.ToString("ddMMyyyHHmmssfffffff");
            foreach (var element in currentDateTime.ToArray())
            {
                result.Append(CHARS[2 * Convert.ToInt32(element.ToString())]);
            }
            return result.ToString();
        }

        private static string GetNewRandomPart(int lenght)
        {
            return new string(
                Enumerable.Repeat(CHARS, lenght)
                    .Select(s => s[Rand(s.Length)])
                    .ToArray());
        }

        private static char ControlSum(List<char> keyChars)
        {
            var result = 4 * keyChars[0] * keyChars[2] / keyChars[1] - keyChars[2] + 27 * keyChars[0];
            result = result % 36;
            return CHARS[result];
        }


        public static string GenerateId(RuntimePlatform targetPlatform, string uniqueData)
        {
            StringBuilder result = new StringBuilder();
            result.Append(GetPlatformSymbol(targetPlatform));
            result.Append("_");
            string part1 = GetNewRandomPart(4);

            int controlSum1Index = 1;
            StringBuilder sb = new StringBuilder(part1);
            sb[controlSum1Index] =
                ControlSum(
                    part1.Select((x, i) => new { x, i }).Where((x, i) => i != controlSum1Index).Select(p => p.x).ToList());
            result.Append(sb.ToString());
            result.Append("-");
            result.Append(uniqueData);
            result.Append("-");
            result.Append(GetNewRandomPart(9));
            result.Append("-");
            result.Append(GetTimestamp());
            return result.ToString();
        }


        public static string GenerateUniqueId()
        {
            string custom_id;
            if (PlayerPrefs.HasKey(_prefsKey) && !String.IsNullOrEmpty(PlayerPrefs.GetString(_prefsKey)))
            {
                custom_id = PlayerPrefs.GetString(_prefsKey);
                if (!String.IsNullOrEmpty(custom_id)) return custom_id;
            }
#if UNITY_ANDROID
            custom_id = GenerateId(Application.platform, GetNewRandomPart(6));
#else
            custom_id = GenerateId(Application.platform, SystemInfo.deviceUniqueIdentifier);
#endif
            PlayerPrefs.SetString(_prefsKey, custom_id);
            return custom_id;
        }
    }
}

