using System;

namespace Stratton.Core
{
    public interface IBindable
    {
        Type[] BindingContractTypes { get; }
    }
}