using System;
using System.Collections.Generic;
using Stratton.Core;
using UnityEngine;

namespace Stratton.Networking
{
    [Serializable]
    public struct NetworkObjectType : IBaseType
    {
        [SerializeField] private string _name;

        public NetworkObjectType(string name)
        {
            _name = name;
        }

        public string Name => _name;
        public bool IsUndefined => _name.IsNullOrEmpty();

        public override string ToString()
        {
            return _name;
        }

        public static bool operator ==(NetworkObjectType b1, NetworkObjectType b2)
        {
            return b1.Equals(b2);
        }

        public static bool operator !=(NetworkObjectType b1, NetworkObjectType b2)
        {
            return !(b1 == b2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var b2 = (NetworkObjectType)obj;
            return Name == b2.Name;
        }

        public override int GetHashCode()
        {
            return -1125283371 + EqualityComparer<string>.Default.GetHashCode(_name);
        }
    }
}


