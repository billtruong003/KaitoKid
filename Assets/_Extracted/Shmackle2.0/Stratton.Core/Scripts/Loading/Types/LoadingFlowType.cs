using System;
using UnityEngine;
using Stratton.Core;
using System.Collections.Generic;

namespace Stratton.Loading.Types
{
    [Serializable]
    public struct LoadingFlowType : IBaseType
    {
        [SerializeField] private string _name;

        public LoadingFlowType(string name)
        {
            _name = name;
        }

        public string Name => _name;
        public bool IsUndefined => _name.IsNullOrEmpty();

        public override string ToString()
        {
            return _name;
        }

        public static bool operator ==(LoadingFlowType b1, LoadingFlowType b2)
        {
            return b1.Equals(b2);
        }

        public static bool operator !=(LoadingFlowType b1, LoadingFlowType b2)
        {
            return !(b1 == b2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            var b2 = (LoadingFlowType)obj;
            return Name == b2.Name;
        }

        public override int GetHashCode()
        {
            return -1125283371 + EqualityComparer<string>.Default.GetHashCode(_name);
        }
    }
}