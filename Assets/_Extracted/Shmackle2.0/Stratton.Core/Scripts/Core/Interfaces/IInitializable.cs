namespace Stratton.Core
{
    /// <summary>
    /// Handles initialization stuff.
    /// </summary>
    public interface IInitializable
    {
        #region Properties

        bool IsReady { get; }

        #endregion

        #region Public Methods

        void Init();
        void DeInit();

        #endregion
    }
}