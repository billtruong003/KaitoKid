using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Stratton.Debugging.UI
{
    public class UICommandSimulatorContainer : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        private SimulatedCommandCollection[] _simulatedCommandCollections;
        [SerializeField]
        private Transform _rootContainer;

        #endregion

        #region Fields

        private List<GameObject> _commandSimulators = new List<GameObject>();

        #endregion

        #region Private Methods

        private void Awake()
        {
            if(!_rootContainer)
            {
                _rootContainer = transform;
            }
        }

        private void Start()
        {
            CreateSimulatedCommandWidgets();
        }

        public void CreateSimulatedCommandWidgets()
        {
            foreach(GameObject simulator in _commandSimulators)
            {
                Destroy(simulator);
            }
            _commandSimulators.Clear();

            foreach (SimulatedCommandCollection collection in _simulatedCommandCollections)
            {
                foreach (SimulatedCommandDisplayInfo info in collection.SimulatedCommandDisplayInfos)
                {
                    GameObject simulator = Instantiate(info.SimulatorPrefab, _rootContainer);
                    _commandSimulators.Add(simulator);
                    ICommandSimulator commandSimulator = simulator.GetComponent<ICommandSimulator>();
                    commandSimulator?.Init(info.SimulationInfo);
                }
            }

        }

        #endregion
    }
}
