using System.Collections.Generic;

namespace Stratton.Core
{
    public static class HasSetExtensions
    {
        #region Public Methods

        public static void AddList<T>(this HashSet<T> set, List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                set.Add(list[i]);
            }
        }

        #endregion
    }
}