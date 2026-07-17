using System;

namespace Stratton.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DataSourceTarget : Attribute
    {
        private readonly string _sourceType;

        public string SourceType => _sourceType;

        public DataSourceTarget(string sourceType)
        {
            _sourceType = sourceType;
        }
    }
}