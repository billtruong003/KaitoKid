using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;

namespace Teabag.UI
{
    /// <summary>
    /// Controls the behavior and interaction logic of the options screen in the UI, allowing users
    /// to configure various gameplay settings, such as audio levels and locomotion modes.
    /// </summary>
    public class OptionScreenController : MonoBehaviour
    {
        private const string OPTIONMUSIC_KEY = "OptionMusic";
        private const string OPTIONSOUND_KEY = "OptionSound";
        private const string OPTIONVOICE_KEY = "OptionVoice";
        private const string TURNING_KEY = "JungleXRKit.TurnMode";

        [SerializeField] private VolumeSliderController sliderMusic;
        [SerializeField] private VolumeSliderController sliderSound;
        [SerializeField] private VolumeSliderController sliderVoice;

        private int _currentTurnMode;
        private IDataPersistenceService _persistence;


        private IHardwareRig LocalHardwareRig =>
            ServiceLocator.TryGet<IRigInfoService>(out var rigInfo) ? rigInfo.HardwareRig : null;


        private void Awake()
        {
            _persistence = ServiceLocator.Get<IDataPersistenceService>();
        }

        private void Start()
        {
            LoadOption();
            SetShowHide(false);
        }

        private void OnEnable()
        {
            LoadOption();
        }

        public void SetShowHide(bool isShow)
        {
            gameObject.SetActive(isShow);
        }

        public void HandleOnClick_ShowHide()
        {
            SetShowHide(!gameObject.activeSelf);
        }

        public void HandleOnClick_ChangeRotateMode(int mode)
        {
            if (!TryGetTurnModule(LocalHardwareRig, out TurnLocomotion turningLocomotionModule)) return;

            _currentTurnMode = mode;
            turningLocomotionModule.TurnMode = (TurnModeType) _currentTurnMode;
        }

        public void HandleOnClick_ChangeContinuousMode(bool isActive)
        {
            var rig = LocalHardwareRig;
            if (rig == null) return;

            if (!TryGetContinuousModule(rig, out var module)) return;

            module.Enabled = false;
        }

        public void HandleOnClick_SetDefault()
        {
            sliderMusic.Amount = 1;
            sliderSound.Amount = 1;
            sliderVoice.Amount = 1;
            _currentTurnMode = 0;

            HandleOnClick_ChangeRotateMode(_currentTurnMode);
        }

        public void SaveOption()
        {
            _persistence.TrySaveData(OPTIONMUSIC_KEY, sliderMusic.Amount);
            _persistence.TrySaveData(OPTIONSOUND_KEY, sliderSound.Amount);
            _persistence.TrySaveData(OPTIONVOICE_KEY, sliderVoice.Amount);
            _persistence.TrySaveData(TURNING_KEY, _currentTurnMode);
        }

        public void LoadOption()
        {
            if (!sliderMusic || !sliderSound || !sliderVoice)
            {
                return;
            }

            sliderMusic.Amount = _persistence.LoadData<float>(OPTIONMUSIC_KEY, 1);
            sliderSound.Amount = _persistence.LoadData<float>(OPTIONSOUND_KEY, 1);
            sliderVoice.Amount = _persistence.LoadData<float>(OPTIONVOICE_KEY, 1);
            _currentTurnMode = _persistence.LoadData<int>(TURNING_KEY, 0);

            HandleOnClick_ChangeRotateMode(_currentTurnMode);
        }

        private bool TryGetTurnModule(IHardwareRig rig, out TurnLocomotion turnModule)
        {
            turnModule = null;
            var locomotionController = rig.LocomotionController;
            if (locomotionController == null) return false;

            locomotionController.GetLocomotionModule(out turnModule);
            return turnModule != null;
        }

        private bool TryGetContinuousModule(IHardwareRig rig, out ContinuousMovement module)
        {
            module = null;
            var locomotionController = rig.LocomotionController;
            if (locomotionController == null) return false;

            locomotionController.GetLocomotionModule(out module);
            return module != null;
        }
    }
}
