using System.Text;
using UnityEngine;

namespace Stratton.Core
{
    public static class ColorExtensions
    {
        #region Public Methods

        public static Texture2D ToTex2D(this Color color)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixels(new Color[1] { color });
            return tex;
        }

        public static string ToHexColor(this Color color)
        {
            int red = Mathf.FloorToInt(color.r * 255f);
            int green = Mathf.FloorToInt(color.g * 255f);
            int blue = Mathf.FloorToInt(color.b * 255f);
 
            StringBuilder sb = new StringBuilder();
            sb.Append(IntToHex(red / 16));
            sb.Append(IntToHex(red % 16));
            sb.Append(IntToHex(green / 16));
            sb.Append(IntToHex(green % 16));
            sb.Append(IntToHex(blue / 16));
            sb.Append(IntToHex(blue % 16));
		
            return sb.ToString();
        }

        public static Color FromHexToColor(this string hexColorString)
        {
            float red = ( HexToInt(hexColorString[1]) + HexToInt(hexColorString[0]) * 16f ) / 255f;
            float green = ( HexToInt(hexColorString[3]) + HexToInt(hexColorString[2]) * 16f ) / 255f;
            float blue = ( HexToInt(hexColorString[5]) + HexToInt(hexColorString[4]) * 16f ) / 255f;
            return new Color(red,green,blue);
        }

        public static string IntToHex(int val)
        {
            const string alphabet = "0123456789ABCDEF";
            return alphabet[val] + "";
        }
	
        public static int HexToInt(char hex)
        {
            switch (hex) 
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                default:
                    Debug.LogError("Unable to parse hex: " + hex);
                    return 0;
            }
        }
 
 
        #endregion
    }
}