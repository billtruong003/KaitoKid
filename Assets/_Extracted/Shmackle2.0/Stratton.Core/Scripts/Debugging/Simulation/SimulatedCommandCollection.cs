using UnityEngine;

namespace Stratton.Debugging
{
    [System.Serializable]
    public struct SimulatedCommandDisplayInfo
    {
        public SimulatedCommandInfo SimulationInfo;
        [Tooltip("Use gameobject with ICommandSimulator in order to control the simulation of this command.")]
        public GameObject SimulatorPrefab;
    }
    

    [CreateAssetMenu(fileName = "DebugCommandCollection", menuName = "Debug/Simulated Command Collection")]
    public class SimulatedCommandCollection : ScriptableObject
    {
        [SerializeField]
        private SimulatedCommandDisplayInfo[] _simulatedCommands;

        public SimulatedCommandDisplayInfo[] SimulatedCommandDisplayInfos => _simulatedCommands;
    }
}
