namespace Stratton.Debugging
{
    public interface ICommandSimulator
    {
        
        #region Public Methods
        
        void Init(SimulatedCommandInfo simulationInfo);

        string GetCommand();

        void Execute();

        #endregion

    }
}