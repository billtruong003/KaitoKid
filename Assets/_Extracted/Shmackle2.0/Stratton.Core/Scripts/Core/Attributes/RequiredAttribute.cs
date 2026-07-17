using UnityEngine;
using System;

namespace Stratton.Core
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class RequiredAttribute : PropertyAttribute
    {
        
    }
}