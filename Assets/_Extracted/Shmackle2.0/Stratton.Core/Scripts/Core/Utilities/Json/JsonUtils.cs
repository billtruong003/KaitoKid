using System;
using Stratton.Core;
using Newtonsoft.Json.Linq;

public static class JsonUtils
{
    public static bool IsValidJson(this string jsonString)
    {
        if (jsonString.IsNullOrEmpty())
        {
            return false;
        }
        jsonString = jsonString.Trim();
        if ((jsonString.StartsWith("{") && jsonString.EndsWith("}")) || // For object
            (jsonString.StartsWith("[") && jsonString.EndsWith("]"))) // For array
        {
            try
            {
                JToken.Parse(jsonString);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        return false;
    }
}