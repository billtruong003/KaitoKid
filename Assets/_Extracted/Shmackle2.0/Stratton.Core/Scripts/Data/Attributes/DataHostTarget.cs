using System;

namespace Stratton.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DataHostTarget : Attribute
    {
        private readonly string _hostType;

        public string HostType => _hostType;

        public DataHostTarget(string hostType)
        {
            _hostType = hostType;
        }
    }
}