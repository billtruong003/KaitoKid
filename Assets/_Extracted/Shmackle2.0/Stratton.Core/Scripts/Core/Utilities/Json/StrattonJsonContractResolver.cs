using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Stratton.Core
{
    public class StrattonJsonContractResolver : DefaultContractResolver
    {
        public static readonly StrattonJsonContractResolver Instance = new StrattonJsonContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyType == typeof(string))
            {
                // Do not include empty strings
                property.ShouldSerialize = instance =>
                {
                    var stringValue = instance.GetType().GetProperty(member.Name).GetValue(instance, null) as string;
                    return !string.IsNullOrWhiteSpace(stringValue);
                };
            }
            return property;
        }
    }
}