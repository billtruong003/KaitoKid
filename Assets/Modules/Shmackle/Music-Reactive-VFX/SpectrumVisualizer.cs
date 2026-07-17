using DG.Tweening;
using UnityEngine;

namespace Hyper.AudioSystems
{
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SpectrumVisualizer : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float updateGate = 0.05f;
        [SerializeField] private float reactionSpeed = 15.0f;
        [SerializeField] private float beatMultiplier = 1.2f;
        [SerializeField] private float fadeOutTime = 1.2f;

        [Header("Visuals")]
        [SerializeField] private Texture2D gradientRamp;

        private AudioSource _audioSource;
        private AnalyzedAudioData _audioData;
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private int _audioIntensityID;
        private int _colorRampID;

        private float _targetFrequency;
        private float _currentDisplayValue;
        private float _nextUpdateTime;

        public float CurrentEnergy => _currentDisplayValue;
        public float CurrentTime => _audioSource != null ? _audioSource.time : 0f;
        public float TotalLength => _audioSource != null && _audioSource.clip != null ? _audioSource.clip.length : 0f;
        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;

        public void Initialize()
        {
            _renderer = GetComponent<Renderer>();
            _audioSource = GetComponent<AudioSource>();

            _propBlock = new MaterialPropertyBlock();
            _audioIntensityID = Shader.PropertyToID("_AudioIntensity");
            _colorRampID = Shader.PropertyToID("_ColorRamp");

            UpdateRampTexture();
            _audioSource.Stop();
        }

        public void PlayFromData(AnalyzedAudioData data, float fadeDuration = 0.5f)
        {
            if (data == null || data.LinkedClip == null) return;

            _audioData = data;
            _audioSource.clip = data.LinkedClip;
            _audioSource.time = 0f;
            _audioSource.volume = 0f;
            _audioSource.Play();
            _audioSource.DOFade(1f, fadeDuration).SetEase(Ease.OutSine);
        }

        public void StopAudio()
        {
            if (_audioSource == null) return;

            _audioSource.DOFade(0f, fadeOutTime).OnComplete(() =>
            {
                _audioSource.Stop();
                UpdateShader(0);
                _audioData = null;
            });
        }

        private void Update()
        {
            if (_audioSource == null || !_audioSource.isPlaying || _audioData == null)
            {
                _targetFrequency = 0f;
            }
            else
            {
                if (Time.time >= _nextUpdateTime)
                {
                    _targetFrequency = _audioData.GetEnergyAtTime(_audioSource.time) * beatMultiplier;
                    _nextUpdateTime = Time.time + updateGate;
                }
            }

            _currentDisplayValue = Mathf.Lerp(_currentDisplayValue, _targetFrequency, Time.deltaTime * reactionSpeed);
            UpdateShader(_currentDisplayValue);
        }

        private void UpdateShader(float value)
        {
            if (_renderer == null) return;
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(_audioIntensityID, value);
            _renderer.SetPropertyBlock(_propBlock);
        }

        private void UpdateRampTexture()
        {
            if (gradientRamp == null || _renderer == null) return;
            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetTexture(_colorRampID, gradientRamp);
            _renderer.SetPropertyBlock(_propBlock);
        }

        public void PlayOneShot(AudioClip clip) => _audioSource.PlayOneShot(clip);
    }
}