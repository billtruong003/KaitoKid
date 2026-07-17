using UnityEngine;

namespace Stratton.Core
{
	public static class NumericExtensions
	{
		#region Public Methods

		public static int ToInt(this double val)
		{
			int res = Mathf.FloorToInt((float)val);
			if(val > 0 && res < 1)
				res = int.MaxValue;
			else if(val < 0 && res > 1)
				res = int.MinValue;
			return res;
		}

		#endregion
	}
}