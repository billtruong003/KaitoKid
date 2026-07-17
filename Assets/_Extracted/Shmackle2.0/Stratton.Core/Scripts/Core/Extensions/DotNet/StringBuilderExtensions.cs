using System.Text;

namespace Stratton.Core
{
    public static class StringBuilderExtensions
    {
        #region Public Methods

        public static StringBuilder Clear(this StringBuilder sb)
        {
            sb.Length = 0;
            return sb;
        }

        #endregion
    }
}