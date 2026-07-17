using System;
using Squido.JungleXRKit.Core;
using TMPro;
using UnityEngine;
using Teabag.Player;
using Teabag.Core;
using IAudioService = Teabag.Core.IAudioService;

namespace Teabag.Customization
{
    public enum SliderAxis
    {
        X,
        Y,
        Z
    }

    public sealed class HSVColourController : MonoBehaviour
    {
        #region Config

        [Header("Slider Nodes")]
        [SerializeField] private Transform _hueNode;
        [SerializeField] private Transform _toneNode;

        [Header("Slider Axis & Range")]
        [SerializeField] private SliderAxis _movementAxis = SliderAxis.Z;
        [SerializeField] private float _sliderMin = -0.001795092f;
        [SerializeField] private float _sliderMax = 0.00263f;

        [Header("Slider Bounds (optional — overrides min/max)")]
        [SerializeField] private Transform _sliderStart;
        [SerializeField] private Transform _sliderEnd;

        [Header("Slider Steps")]
        [SerializeField] private int _hueSteps = 20;
        [SerializeField] private int _toneSteps = 10;

        [Header("Smart Tone")]
        [Range(0f, 1f)]
        [SerializeField] private float _pastelSaturation = 0.70f;

        [Header("Text Displays")]
        [SerializeField] private TMP_Text _hueText;
        [SerializeField] private TMP_Text _toneText;
        [SerializeField] private TMP_Text _partText;

        [Header("Strip Visuals (Unlit — color via MPB)")]
        [SerializeField] private Renderer _hueSliderBase;
        [SerializeField] private int _hueStripMaterialIndex;
        [SerializeField] private Renderer _toneSliderBase;
        [SerializeField] private int _toneStripMaterialIndex;

        [Header("Target Button (TargetPartIcon shader)")]
        [SerializeField] private Renderer _targetButtonRenderer;
        [SerializeField] private int _targetButtonMaterialIndex;

        [Header("Audio")]
        [SerializeField] private AdvancedAudioClip _tick;
        [SerializeField] private AdvancedAudioClip _partSwitchClip;

        [Header("Interaction")]
        [SerializeField] private float _interactionRadius = 0.05f;
        [Tooltip("Minimum trigger pull (0-1) required to grab a slider")]
        [Range(0f, 1f)]
        [SerializeField] private float _triggerThreshold = 0.5f;

        [Header("Parts")]
        [SerializeField] private int _maxPartIndex = 2;
        [SerializeField] private string[] _partDisplayNames = { "Body", "Head", "Arms" };

        #endregion

        private int PartCount => _maxPartIndex + 1;

        private IGorillaService _gorillaService;
        private IDataPersistenceService _persistence;
        private IAudioService _audioService;

        private BodyPart _currentPart = BodyPart.PartR;

        private int[] _huePerPart;
        private int[] _tonePerPart;
        private int _lastHueStep;
        private int _lastToneStep;

        private Transform _leftGrabbedNode;
        private Transform _rightGrabbedNode;

        private static readonly int PropBaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int PropSliceIndex = Shader.PropertyToID("_SliceIndex");
        private static readonly int PropIsSelected = Shader.PropertyToID("_IsSelected");

        private MaterialPropertyBlock _hueMPB;
        private MaterialPropertyBlock _toneMPB;
        private MaterialPropertyBlock _targetMPB;

        private Action<int, bool> _onHueChanged;
        private Action<int, bool> _onToneChanged;

        private float _interactionRadiusSqr;
        private float _releaseRadiusSqr;

        private string[] _stepStrings;
        private string[][] _persistKeys;

        private float GetAxisValue(Vector3 v)
        {
            return _movementAxis switch
            {
                SliderAxis.X => v.x,
                SliderAxis.Y => v.y,
                _ => v.z
            };
        }

        private void SetAxisValue(ref Vector3 v, float val)
        {
            switch (_movementAxis)
            {
                case SliderAxis.X: v.x = val; break;
                case SliderAxis.Y: v.y = val; break;
                default: v.z = val; break;
            }
        }

        private int GetSliderStep(Transform node, int maxSteps)
        {
            float t = Mathf.InverseLerp(_sliderMin, _sliderMax, GetAxisValue(node.localPosition));
            return Mathf.Clamp(Mathf.RoundToInt(t * maxSteps), 0, maxSteps);
        }

        private void SetSliderStep(Transform node, int step, int maxSteps)
        {
            float target = Mathf.Lerp(_sliderMin, _sliderMax, (float)step / maxSteps);
            Vector3 pos = node.localPosition;
            SetAxisValue(ref pos, target);
            node.localPosition = pos;
        }

        private void SmartToneCurve(float t, out float saturation, out float value)
        {
            float tLower = Mathf.Clamp01(t * 2f);
            float tUpper = Mathf.Clamp01((t - 0.5f) * 2f);
            value = tLower;
            saturation = Mathf.Lerp(_pastelSaturation, 0f, tUpper);
        }

        private Color HSVFromSteps(int hueStep, int toneStep)
        {
            float h = (float)hueStep / _hueSteps;
            float t = (float)toneStep / _toneSteps;
            SmartToneCurve(t, out float s, out float v);
            return Color.HSVToRGB(h, s, v);
        }

        private void Awake()
        {
            _persistence = ServiceLocator.Get<IDataPersistenceService>();
            _audioService = ServiceLocator.Get<IAudioService>();
            _gorillaService = ServiceLocator.Get<IGorillaService>();

            _hueMPB = new MaterialPropertyBlock();
            _toneMPB = new MaterialPropertyBlock();
            _targetMPB = new MaterialPropertyBlock();

            _onHueChanged = OnHueStepChanged;
            _onToneChanged = OnToneStepChanged;

            _interactionRadiusSqr = _interactionRadius * _interactionRadius;
            _releaseRadiusSqr = _interactionRadiusSqr * 2.25f;

            if (_sliderStart && _sliderEnd)
            {
                float a = GetAxisValue(_sliderStart.localPosition);
                float b = GetAxisValue(_sliderEnd.localPosition);
                _sliderMin = Mathf.Min(a, b);
                _sliderMax = Mathf.Max(a, b);
            }

            int maxStep = Mathf.Max(_hueSteps, _toneSteps) + 1;
            _stepStrings = new string[maxStep];
            for (int i = 0; i < maxStep; i++)
                _stepStrings[i] = i.ToString();

            int partCount = PartCount;
            _huePerPart = new int[partCount];
            _tonePerPart = new int[partCount];
            _persistKeys = new string[partCount][];

            for (int i = 0; i < partCount; i++)
            {
                BodyPart part = (BodyPart)i;
                _persistKeys[i] = new[]
                {
                    $"Colour_{part}_Hue",
                    $"Colour_{part}_Tone"
                };

                float savedHue = _persistence?.LoadData<float>(_persistKeys[i][0], 0f) ?? 0f;
                float savedTone = _persistence?.LoadData<float>(_persistKeys[i][1], 0.5f) ?? 0.5f;

                _huePerPart[i] = Mathf.RoundToInt(savedHue * _hueSteps);
                _tonePerPart[i] = Mathf.RoundToInt(savedTone * _toneSteps);
            }

            LoadPartToSliders();
            RefreshStripVisuals();
            RefreshTargetButton();
            ApplyAllPartsToMaterial();
        }

        private void Update()
        {
            if (_gorillaService?.LocalGorilla is not Gorilla localGorilla) return;

            Vector3 leftFinger = localGorilla.leftHand.finger.transform.position;
            Vector3 rightFinger = localGorilla.rightHand.finger.transform.position;

            bool leftTriggerHeld = VRInputHandler.GetInputDownAmount(true, InputType.Trigger) >= _triggerThreshold;
            bool rightTriggerHeld = VRInputHandler.GetInputDownAmount(false, InputType.Trigger) >= _triggerThreshold;

            UpdateHandGrab(ref _leftGrabbedNode, leftFinger, true, leftTriggerHeld);
            UpdateHandGrab(ref _rightGrabbedNode, rightFinger, false, rightTriggerHeld);
        }

        private void UpdateHandGrab(ref Transform grabbedNode, Vector3 fingerPos, bool isLeftHand, bool triggerHeld)
        {
            if (grabbedNode == null)
            {
                if (!triggerHeld) return;

                float hueSqr = (_hueNode.position - fingerPos).sqrMagnitude;
                float toneSqr = (_toneNode.position - fingerPos).sqrMagnitude;

                bool hueAvailable = _hueNode != _leftGrabbedNode && _hueNode != _rightGrabbedNode;
                bool toneAvailable = _toneNode != _leftGrabbedNode && _toneNode != _rightGrabbedNode;

                float minDist = float.MaxValue;

                if (hueAvailable && hueSqr < _interactionRadiusSqr)
                {
                    minDist = hueSqr;
                    grabbedNode = _hueNode;
                }

                if (toneAvailable && toneSqr < _interactionRadiusSqr && toneSqr < minDist)
                {
                    grabbedNode = _toneNode;
                }
            }

            if (grabbedNode == null) return;

            float dist = (grabbedNode.position - fingerPos).sqrMagnitude;
            if (!triggerHeld || dist > _releaseRadiusSqr)
            {
                grabbedNode = null;
                return;
            }

            if (grabbedNode == _hueNode)
                ProcessSlider(_hueNode, _hueSteps, ref _lastHueStep, fingerPos, isLeftHand, _onHueChanged);
            else if (grabbedNode == _toneNode)
                ProcessSlider(_toneNode, _toneSteps, ref _lastToneStep, fingerPos, isLeftHand, _onToneChanged);
        }

        private void ProcessSlider(
            Transform node, int maxSteps, ref int lastStep,
            Vector3 fingerPos, bool isLeftHand,
            Action<int, bool> onStepChanged)
        {
            Vector3 lastLocalPos = node.localPosition;

            node.position = fingerPos;

            Vector3 newPos = lastLocalPos;
            float raw = GetAxisValue(node.localPosition);
            SetAxisValue(ref newPos, Mathf.Clamp(raw, _sliderMin, _sliderMax));
            node.localPosition = newPos;

            Vector3 delta = node.localPosition - lastLocalPos;
            if (delta.sqrMagnitude > 0f)
                VRInputHandler.VibrateController(isLeftHand, delta.magnitude * 1000f, 0.1f);

            int currentStep = GetSliderStep(node, maxSteps);
            if (currentStep == lastStep) return;

            lastStep = currentStep;
            onStepChanged(currentStep, isLeftHand);
        }

        private void OnHueStepChanged(int newStep, bool isLeftHand)
        {
            _huePerPart[(int)_currentPart] = newStep;

            VRInputHandler.VibrateController(isLeftHand, 0.1f, 0.1f);
            _audioService?.Play(_tick, _hueNode.position);

            if (_hueText) _hueText.text = _stepStrings[newStep];

            SaveCurrentPart();
            RefreshStripVisuals();
            ApplyCurrentPartToMaterial();
        }

        private void OnToneStepChanged(int newStep, bool isLeftHand)
        {
            _tonePerPart[(int)_currentPart] = newStep;

            VRInputHandler.VibrateController(isLeftHand, 0.1f, 0.1f);
            _audioService?.Play(_tick, _toneNode.position);

            if (_toneText) _toneText.text = _stepStrings[newStep];

            SaveCurrentPart();
            RefreshToneStrip();
            ApplyCurrentPartToMaterial();
        }

        private void RefreshStripVisuals()
        {
            RefreshHueStrip();
            RefreshToneStrip();
        }

        private void RefreshHueStrip()
        {
            if (!_hueSliderBase) return;

            float h = (float)_huePerPart[(int)_currentPart] / _hueSteps;
            Color hueColor = Color.HSVToRGB(h, _pastelSaturation, 1f);

            _hueSliderBase.GetPropertyBlock(_hueMPB, _hueStripMaterialIndex);
            _hueMPB.SetColor(PropBaseColor, hueColor);
            _hueSliderBase.SetPropertyBlock(_hueMPB, _hueStripMaterialIndex);
        }

        private void RefreshToneStrip()
        {
            if (!_toneSliderBase) return;

            int idx = (int)_currentPart;
            Color toneColor = HSVFromSteps(_huePerPart[idx], _tonePerPart[idx]);

            _toneSliderBase.GetPropertyBlock(_toneMPB, _toneStripMaterialIndex);
            _toneMPB.SetColor(PropBaseColor, toneColor);
            _toneSliderBase.SetPropertyBlock(_toneMPB, _toneStripMaterialIndex);
        }

        public void CycleTargetPart()
        {
            _currentPart = (BodyPart)(((int)_currentPart + 1) % PartCount);

            LoadPartToSliders();
            RefreshStripVisuals();
            RefreshTargetButton();

            _audioService?.Play(_partSwitchClip, transform.position);
        }

        private void RefreshTargetButton()
        {
            if (!_targetButtonRenderer) return;

            _targetButtonRenderer.GetPropertyBlock(_targetMPB, _targetButtonMaterialIndex);
            _targetMPB.SetFloat(PropSliceIndex, (int)_currentPart);
            _targetMPB.SetFloat(PropIsSelected, 1f);
            _targetButtonRenderer.SetPropertyBlock(_targetMPB, _targetButtonMaterialIndex);

            if (!_partText) return;
            int idx = (int)_currentPart;
            _partText.text = idx < _partDisplayNames.Length ? _partDisplayNames[idx] : _stepStrings[idx];
        }

        private void LoadPartToSliders()
        {
            int idx = (int)_currentPart;
            SetSliderStep(_hueNode, _huePerPart[idx], _hueSteps);
            SetSliderStep(_toneNode, _tonePerPart[idx], _toneSteps);

            _lastHueStep = _huePerPart[idx];
            _lastToneStep = _tonePerPart[idx];

            if (_hueText) _hueText.text = _stepStrings[_huePerPart[idx]];
            if (_toneText) _toneText.text = _stepStrings[_tonePerPart[idx]];
        }

        private void SaveCurrentPart()
        {
            int idx = (int)_currentPart;
            _persistence?.TrySaveData(_persistKeys[idx][0], (float)_huePerPart[idx] / _hueSteps);
            _persistence?.TrySaveData(_persistKeys[idx][1], (float)_tonePerPart[idx] / _toneSteps);
        }

        private void ApplyCurrentPartToMaterial()
        {
            if (_gorillaService?.LocalGorilla is not Gorilla localGorilla || localGorilla.material == null) return;

            int idx = (int)_currentPart;
            Color rgb = HSVFromSteps(_huePerPart[idx], _tonePerPart[idx]);
            localGorilla.material.SetPartColour(_currentPart, rgb);
        }

        private void ApplyAllPartsToMaterial()
        {
            if (_gorillaService?.LocalGorilla is not Gorilla localGorilla || localGorilla.material == null) return;

            int partCount = PartCount;
            for (int i = 0; i < partCount; i++)
            {
                Color rgb = HSVFromSteps(_huePerPart[i], _tonePerPart[i]);
                localGorilla.material.SetPartColour((BodyPart)i, rgb);
            }
        }

        private void OnDrawGizmosSelected()
        {
            float releaseRadius = _interactionRadius * 1.5f;

            if (_hueNode)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_hueNode.position, _interactionRadius);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_hueNode.position, releaseRadius);
            }

            if (_toneNode)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_toneNode.position, _interactionRadius);
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_toneNode.position, releaseRadius);
            }

            if (_sliderStart && _sliderEnd)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_sliderStart.position, _sliderEnd.position);
                Gizmos.DrawWireSphere(_sliderStart.position, 0.005f);
                Gizmos.DrawWireSphere(_sliderEnd.position, 0.005f);
            }
        }
    }
}