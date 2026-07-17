namespace Stratton.Core
{
    public interface IVersionableDataModel
    {
        #region Properties

        public int Hash { get; }

        #endregion

        #region Public Methods

        int GetHashCode();
        string ToJsonString();

        #endregion
    }
}