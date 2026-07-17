using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Core.Editor
{
    public static class UnityEditorStyles
    {
        #region Fields

        private static GUIStyle _centeredMiniLabel;
        private static GUIStyle _centeredMiniBoldLabel;

        #endregion

        #region Properties

        /// <summary>
        ///     Style for a label with small, regular font.
        /// </summary>
        public static GUIStyle CenteredMiniLabel
        {
            get
            {
                if (_centeredMiniLabel == null)
                {
                    _centeredMiniLabel = new GUIStyle("miniLabel");
                    _centeredMiniLabel.alignment = TextAnchor.MiddleCenter;
                }

                return _centeredMiniLabel;
            }
        }

        /// <summary>
        ///     Style for a label with small, bold font.
        /// </summary>
        public static GUIStyle CenteredMiniBoldLabel
        {
            get
            {
                if (_centeredMiniBoldLabel == null)
                {
                    _centeredMiniBoldLabel = new GUIStyle("MiniBoldLabel");
                    _centeredMiniBoldLabel.alignment = TextAnchor.MiddleCenter;
                }

                return _centeredMiniBoldLabel;
            }
        }

        #endregion
    }
}