using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Stratton.Core
{
    public class DictionaryUnityTypeConverter : UnityTypeConverter
    {
        public override bool CanConvert(Type objectType)
        {
            if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return IsUnityType(objectType.GetGenericArguments()[0]);
            }

            return false;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var dictionary = (System.Collections.IDictionary)value;

            writer.WriteStartObject();

            foreach (var key in dictionary.Keys)
            {
                string keyString = SerializeUnityType(key);
                writer.WritePropertyName(keyString);
                serializer.Serialize(writer, dictionary[key]);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var keyType = objectType.GetGenericArguments()[0];
            var valueType = objectType.GetGenericArguments()[1];

            var dictionary = (System.Collections.IDictionary)Activator.CreateInstance(objectType);

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    string keyString = (string)reader.Value;
                    var key = DeserializeUnityType(keyString, keyType);

                    reader.Read();
                    var value = serializer.Deserialize(reader, valueType);

                    dictionary.Add(key, value);
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    break;
                }
            }

            return dictionary;
        }
    }
}


