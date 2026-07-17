using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Stratton.Core.Editor
{
    [System.Serializable]
    public class MakroData
    {
        //Scene
        public enum MakroActionType
        {
            PlayWithoutSaving,
            PlayWithSaving,
            OpenWithSaving,
            OpenWithoutSaving
        }

        public enum MakroTargetType
        {
            Scene,
            Script
        }

        #region Fields

        public string ButtonName;
        public MakroTargetType TargetType;
        public Object Target;

        //Need to be in one class because of serialisation 
        //Script
        public string ComponentName;
        public string MethodName;
        public MakroActionType Action;

        [HideInInspector] public bool Show;

        #endregion
    }
}