namespace Stratton.Core
{
    /// <summary>
    ///     Interface for GameObjects which shouldn't be destroyed, when a new scene is loaded.
    /// </summary>
    /// <remarks>
    ///     Purpose of this interface is to keep track on every automatically undestroyable GameObject.
    /// </remarks>
    public interface IDontDestroyOnLoad
    {
        #region Public Methods

        /// <summary>
        ///     Marks whole <see cref="UnityEngine.Component.gameObject" /> as
        ///     <see cref="UnityEngine.Object.DontDestroyOnLoad" />.
        ///     This method should be called during Awake().
        /// </summary>
        void MarkAsDontDestroyOnLoad();

        #endregion
    }
}