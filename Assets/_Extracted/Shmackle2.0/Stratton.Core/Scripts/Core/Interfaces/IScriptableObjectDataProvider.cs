using UnityEngine;

namespace Stratton.Core
{
    public interface IScriptableObjectDataProvider
    {
        bool TryGetData<T>(out T data) where T : ScriptableObject;
    }

}
