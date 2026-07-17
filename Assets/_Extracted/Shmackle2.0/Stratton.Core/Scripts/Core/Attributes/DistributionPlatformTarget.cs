using System;

namespace Stratton.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DistributionPlatformTarget : Attribute
    {
        private readonly string _platformTypeName;

        public string PlatformTypeName => _platformTypeName;

        public DistributionPlatformTarget(string platformTypeName)
        {
            _platformTypeName = platformTypeName;
        }
    }
}