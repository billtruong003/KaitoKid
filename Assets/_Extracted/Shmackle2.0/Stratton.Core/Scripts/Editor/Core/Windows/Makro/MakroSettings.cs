using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Core.Editor
{
    public class MakroSettings : ScriptableObject
    {
        #region Fields

        public const string SettingsDirectory = "Assets/Settings/Editor/";
        public const string SettingsFileName = "MakroSettings.asset";

        public const string MakroSettingsPath = SettingsDirectory + SettingsFileName;

        public string MakroToolBarScriptPath = "Scripts/Editor/Makro/MakroToolBar.cs";

        public List<MakroData> Data = new List<MakroData>();

        #endregion
    }
}