using GatorDragonGames.JigglePhysics;
using NaughtyAttributes;
using Player.Config;
using UnityEngine;

namespace Prefabs.Player.Temp
{
    public class JiggleRigController : MonoBehaviour
    {
        [SerializeField] private BootyJiggleConfig _bootyJiggleConfig;

        // References to the left and right JiggleRig components this controller will drive.
        [SerializeField] private JiggleRig _leftJiggleRig;
        [SerializeField] private JiggleRig _rightJiggleRig;

        private float _gravity = 0.1f;
        private float _ignoreRootMotion = 0.5f;
        private float _stretch = 0.5f;
        private float _stiffness = 1f;

        private void OnEnable()
        {
            if (_bootyJiggleConfig) _bootyJiggleConfig.ConfigUpdated += ConfigUpdated;
        }

        private void OnDisable()
        {
            if (_bootyJiggleConfig) _bootyJiggleConfig.ConfigUpdated -= ConfigUpdated;
        }

        private void ConfigUpdated()
        {
            if (!_bootyJiggleConfig)
                return;

            _gravity = _bootyJiggleConfig.JiggleGravity;
            _ignoreRootMotion = _bootyJiggleConfig.JiggleIgnoreRootMotion;
            _stiffness = _bootyJiggleConfig.JiggleStiffness;
            _stretch = _bootyJiggleConfig.JiggleStretch;

            ClickUpdateRuntime();
        }

        private void Start()
        {
            if (_bootyJiggleConfig)
                ConfigUpdated();
        }

        public void EnableJiggle(bool show)
        {
            if (_leftJiggleRig) _leftJiggleRig.gameObject.SetActive(show);
            if (_rightJiggleRig) _rightJiggleRig.gameObject.SetActive(show);
        }

        // Inspector button (via NaughtyAttributes) to push the current runtime values
        // of gravity and ignoreRootMotion into both left and right JiggleRigs.
        [Button]
        private void ClickUpdateRuntime()
        {
            // Update both the left and right jiggle rigs using the configured runtime values.
            if (_leftJiggleRig) UpdateRuntime(_leftJiggleRig);
            if (_rightJiggleRig) UpdateRuntime(_rightJiggleRig);
        }

        // Applies the configured gravity and ignoreRootMotion values to a single JiggleRig instance.
        private void UpdateRuntime(JiggleRig rig)
        {
            if (!rig)
                return;

            // Get a copy of the JiggleRigData struct from the JiggleRig.
            var jigData = rig.GetJiggleRigData();

            // Work on a local copy of the input parameters struct.
            var jigInputParams = jigData.jiggleTreeInputParameters;

            // Set gravity and ignore root motion using the serialized runtime values.
            jigInputParams.gravity.value = _gravity;
            jigInputParams.ignoreRootMotion = _ignoreRootMotion;
            jigInputParams.stiffness = new JiggleTreeCurvedFloat(_stiffness);
            jigInputParams.stretch = new JiggleTreeCurvedFloat(_stretch);

            // Write the modified parameters back into the JiggleRigData struct.
            jigData.jiggleTreeInputParameters = jigInputParams;

            // Set the JiggleRig's input parameters to the modified struct.'
            rig.SetInputParameters(jigInputParams);

            // Notify the JiggleRig to push these updated parameters into the runtime jiggle tree.
            rig.UpdateParameters();
        }
    }
}