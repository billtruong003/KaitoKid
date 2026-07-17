using System;
using Stratton.Core;
using UnityEditor;

namespace Stratton.Core.Editor
{
    [CustomPropertyDrawer(typeof(UnityDateTime))]
    public class UnityDateTimeDrawer : StringFormatedPropertyDrawer
    {
        protected override SerializedPropertyType PropertyType
        {
            get { return SerializedPropertyType.String; }
        }

        protected override object StringToData(string str)
        {
            return str;
        }

        protected override bool IsValid(string s)
        {
            UnityDateTime unityDateTime;
            return UnityDateTime.TryParse(s, out unityDateTime);
        }

        protected override string DataToString(object objectValue)
        {
            return objectValue.ToString();
        }


        protected override string SerializedPropertyName
        {
            get { return "days"; }
        }

        protected override string ErrorMessage
        {
            get { return "Invalid input date."; }
        }

        protected override string CorrectMessage
        {
            get { return String.Format("OK: {0}", UnityDateTime.Parse(DisplayedString).DateTime); }
        }
    }
}
