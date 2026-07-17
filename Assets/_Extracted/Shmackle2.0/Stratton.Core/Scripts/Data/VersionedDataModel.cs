using System;
using System.Collections.Generic;
using Stratton.Core;
using Newtonsoft.Json;

namespace Stratton.Data
{
    [Serializable]
    public abstract class VersionedDataModel : IVersionableDataModel
    {
        #region Properties

        public int Hash { get; set; }

        #endregion

        #region Public Methods

        public override int GetHashCode()
        {
            return 0;
        }

        public virtual string ToJsonString()
        {
            Hash = GetHashCode();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            //settings.NullValueHandling = NullValueHandling.Ignore;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            //settings.ContractResolver = StrattonJsonContractResolver.Instance;
            string jsonString = JsonConvert.SerializeObject(this, settings);
            Dictionary<string, object> json = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
            return JsonConvert.SerializeObject(json, settings);
        }

        public static T FromJsonString<T>(string jsonString) where T : IVersionableDataModel
        {
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        public static object? FromJsonString(string jsonString, Type type)
        {
            return JsonConvert.DeserializeObject(jsonString, type);
        }

        #endregion
    }
}