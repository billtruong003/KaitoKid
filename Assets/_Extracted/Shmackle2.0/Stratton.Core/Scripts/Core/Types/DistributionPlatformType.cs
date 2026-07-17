using System;
using UnityEngine;
using Stratton.Core;

namespace Stratton.Core.Types
{
    [Serializable]
    public struct DistributionPlatformType : IBaseType
    {
        [SerializeField] private string _name;

        public DistributionPlatformType(string name)
        {
            _name = name;
        }

        public string Name => _name;
        public bool IsUndefined => _name.IsNullOrEmpty();

        public override string ToString()
        {
            return _name;
        }

        public static bool operator ==(DistributionPlatformType b1, DistributionPlatformType b2)
        {
            return b1.Equals(b2);
        }

        public static bool operator !=(DistributionPlatformType b1, DistributionPlatformType b2)
        {
            return !(b1 == b2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var b2 = (DistributionPlatformType)obj;
            return Name == b2.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}