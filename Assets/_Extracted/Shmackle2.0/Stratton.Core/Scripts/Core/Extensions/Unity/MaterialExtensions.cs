using UnityEngine;

namespace Stratton.Core
{
    public static class MaterialExtensions
    {
        #region Public Methods

        public static void LoadTextureIntoGPU(this Material mat, string paramName)
        {
            if (!mat.HasProperty(paramName))
            {
                Debug.LogError(string.Format("Material \"{0}\" doesn't have texture property of name \"{1}\"",
                    mat, paramName));
                return;
            }

            Texture tex = mat.GetTexture(paramName);

            if (tex)
            {
                tex.LoadIntoGPU();
            }
            else
            {
                Debug.LogError(string.Format("Material \"{0}\" have texture property of name \"{1}\", but texture is null!",
                    mat, paramName));
            }
        }

        #endregion
    }
}