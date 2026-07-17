using System;
using System.Diagnostics;

namespace Stratton.Core
{
    /// <summary>
    ///     Can be use to measure time of some code in cool way.
    /// </summary>
    /// <example>
    ///     <code>
    ///     using(new PerformanceTimer("MeasureName")
    ///     {
    ///         //code
    ///     }
    ///     </code>
    /// </example>
    public class PerformanceTimer : IDisposable
    {
        #region Fields

        private readonly string _text;
        private readonly Stopwatch _stopwatch;

        #endregion

        #region Constructors

        public PerformanceTimer(string text)
        {
            _text = text;
            _stopwatch = Stopwatch.StartNew();
        }

        #endregion

        #region Public Methods

        public void Dispose()
        {
            _stopwatch.Stop();
            UnityEngine.Debug.Log(string.Format("Profiled {0}: {1:0.00}ms", _text, _stopwatch.ElapsedMilliseconds));
        }

        #endregion
    }
}