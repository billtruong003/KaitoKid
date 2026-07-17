using Shmackle.Logging;
using TMPro;
using UnityEngine;

namespace Shmackle.Debugging
{
    public class DebugFrameTimeCanvas : MonoBehaviour
    {
        [SerializeField] 
        private DebugRuntimeData _debugRuntimeData;
        
        [SerializeField]
        private TMP_Text _dummyProtobroCountText;
        
        [SerializeField]
        private TMP_Text _frameTimeText;

        private int _prevCount;
        private float _prevFrameTime = -1f;
        private bool _isRed;

        private void Update()
        {
            if (_debugRuntimeData)
            {
                var frameTime = _debugRuntimeData.FrameTime;

                // Only update text if frame time changed (with tolerance to avoid micro-updates)
                if (Mathf.Abs(frameTime - _prevFrameTime) > 1f)
                {
                    _frameTimeText.text = frameTime.ToString("0.00");
                    _prevFrameTime = frameTime;
                }

                // Only update color when crossing the threshold
                bool shouldBeRed = frameTime > 12;
                if (shouldBeRed != _isRed)
                {
                    _frameTimeText.color = shouldBeRed ? Color.red : Color.white;
                    _isRed = shouldBeRed;
                }
            }
            
        }

        public void SetDummyProtobroCount(int count)
        {
            if (_prevCount == count) return;
            _dummyProtobroCountText.text = $"{count}";
            _prevCount = count;
        }
    }
}