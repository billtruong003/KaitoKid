using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Core
{
    public class NotCachedAddressableException : Exception
    {
        public NotCachedAddressableException(string message): base(message)
        {

        }
    }
}
