using UnityEngine;

namespace Stratton.Core
{
    public static class TextureExtensions
    {
        #region Public Methods

        public static void LoadIntoGPU(this Texture tex)
        {
            if (tex != null)
            {
                int w = tex.width;
            }
        }

        #endregion
    }
}