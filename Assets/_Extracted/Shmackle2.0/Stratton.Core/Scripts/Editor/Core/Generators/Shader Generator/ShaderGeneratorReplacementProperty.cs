namespace Stratton.Core.Editor
{
    [System.Serializable]
    public class ShaderGeneratorReplacementProperty
    {
        #region Fields

        public int SelectedValue;
        public string Key;
        public string[] Values;
        public string[] ValueNames;

        #endregion

        #region Constructors

        public ShaderGeneratorReplacementProperty(string key, string[] values, string[] valueNames)
        {
            Key = key;
            Values = values;
            ValueNames = valueNames;
        }

        #endregion

        #region Public Methods

        public string ReplaceTextWithSelectedValue(string text)
        {
            return text.Replace(Key, Values[SelectedValue]);
        }

        public void DrawGUI()
        {
            SelectedValue = InspectorEditor.Popup(SelectedValue, Key, ValueNames);
        }

        #endregion
    }
}