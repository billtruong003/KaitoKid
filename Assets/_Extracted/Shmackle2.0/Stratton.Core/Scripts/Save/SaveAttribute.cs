using System;

namespace Stratton.Save
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SaveAttribute : Attribute
    {
        public string Key { get; set; } = "";
        public bool IsNested { get; set; } = false;
        public SavePattern SavePattern { get; set; } = SavePattern.OnValueChange;

        public SaveAttribute(string key = "", bool isNested = false, SavePattern savePattern = SavePattern.OnValueChange)
        {
            Key = key;
            IsNested = isNested;
            SavePattern = savePattern;
        }
    }
}


