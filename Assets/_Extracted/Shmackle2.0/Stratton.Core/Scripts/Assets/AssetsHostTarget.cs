using System;

namespace Stratton.Assets
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AssetsHostTarget : Attribute
    {
        private readonly string _hostType;

        public string HostType => _hostType;

        public AssetsHostTarget(string hostType)
        {
            _hostType = hostType;
        }
    }
}