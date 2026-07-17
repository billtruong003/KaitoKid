using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Stratton.Core
{
    public class UnityTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return IsUnityType(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            string serializedValue = SerializeUnityType(value);
            if (serializedValue == null)
            {
                throw new JsonSerializationException($"Unsupported type: {value.GetType()}");
            }

            writer.WriteValue(serializedValue);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonToken.String)
            {
                throw new JsonSerializationException($"Unexpected token parsing Unity type. Expected String, got {reader.TokenType}.");
            }

            string value = (string)reader.Value;
            return DeserializeUnityType(value, objectType);
        }

        protected bool IsUnityType(Type type)
        {
            return type == typeof(Vector2) ||
                   type == typeof(Vector2Int) ||
                   type == typeof(Vector3) ||
                   type == typeof(Vector3Int) ||
                   type == typeof(Quaternion) ||
                   type == typeof(Color) ||
                   type == typeof(Color32);
        }

        protected string SerializeUnityType(object value)
        {
            switch (value)
            {
                case Vector2 v2:
                    return $"{v2.x},{v2.y}";
                case Vector2Int v2Int:
                    return $"{v2Int.x},{v2Int.y}";
                case Vector3 v3:
                    return $"{v3.x},{v3.y},{v3.z}";
                case Vector3Int v3Int:
                    return $"{v3Int.x},{v3Int.y},{v3Int.z}";
                case Quaternion q:
                    return $"{q.x},{q.y},{q.z},{q.w}";
                case Color c:
                    return $"{c.ToHexColor()}";
                case Color32 c32:
                    return $"{c32.r},{c32.g},{c32.b},{c32.a}";
                default:
                    return null;
            }
        }

        protected object DeserializeUnityType(string value, Type objectType)
        {
            string[] parts = value.Split(',');

            try
            {
                if (objectType == typeof(Vector2))
                {
                    return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
                }
                else if (objectType == typeof(Vector2Int))
                {
                    return new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
                }
                else if (objectType == typeof(Vector3))
                {
                    return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                }
                else if (objectType == typeof(Vector3Int))
                {
                    return new Vector3Int(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
                }
                else if (objectType == typeof(Quaternion))
                {
                    return new Quaternion(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3])
                    );
                }
                else if (objectType == typeof(Color))
                {
                    return value.FromHexToColor();
                }
                else if (objectType == typeof(Color32))
                {
                    return new Color32(
                        byte.Parse(parts[0]),
                        byte.Parse(parts[1]),
                        byte.Parse(parts[2]),
                        byte.Parse(parts[3])
                    );
                }
            }
            catch (Exception ex)
            {
                throw new JsonSerializationException($"Error parsing Unity type {objectType}: {value}", ex);
            }

            throw new JsonSerializationException($"Unsupported Unity type: {objectType}");
        }
    }
}


