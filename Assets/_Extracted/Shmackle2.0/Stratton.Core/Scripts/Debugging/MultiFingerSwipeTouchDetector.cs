using System.Collections.Generic;
using Opencoding.Console;
using Opencoding.Console.TouchDetectors;
using UnityEngine;

namespace Stratton.Debugging
{
    public class MultiFingerSwipeTouchDetector : TouchDetector
    {
		private readonly Dictionary<int, Vector2> _initialTouchPositions = new Dictionary<int, Vector2>();
		private readonly Dictionary<int, Vector2> _maximumOffsetPosition = new Dictionary<int, Vector2>();
		private int _fingersCount;
		private float _touchStartTime;

        public MultiFingerSwipeTouchDetector(int fingersCount)
		{
			_fingersCount = fingersCount;
        }

		public bool Update()
		{
			bool shouldShowConsole = false;
			Touch[] touches = UnityEngine.Input.touches;
			for (int i = 0; i < touches.Length; i++)
			{
				Touch touch = touches[i];
				TouchPhase phase = touch.phase;
				switch (phase)
				{
					case TouchPhase.Began:
						HandleTouchBegan(touch);
						break;
					case TouchPhase.Moved:
						if (_initialTouchPositions.ContainsKey(touch.fingerId))
						{
							HandleTouchMoved(touch);
							break;
						}
						goto case 0;
					case TouchPhase.Ended:
						HandleTouchEnded(touch, ref shouldShowConsole);
						break;
					case TouchPhase.Canceled:
						HandleTouchCanceled(touch);
						break;
				}
			}
			return shouldShowConsole;
		}

		private void HandleTouchBegan(Touch touch)
		{
			_maximumOffsetPosition.Remove(touch.fingerId);
			_initialTouchPositions[touch.fingerId] = touch.position;
			_touchStartTime = UnityEngine.Time.realtimeSinceStartup;
        }

		private void HandleTouchMoved(Touch touch)
		{
			Vector2 val = _initialTouchPositions[touch.fingerId];
			if (!_maximumOffsetPosition.TryGetValue(touch.fingerId, out var value))
			{
				value = val;
			}
			float num = Vector2.Distance(val, value);
			if (Vector2.Distance(val, touch.position) > num)
			{
				_maximumOffsetPosition[touch.fingerId] = touch.position;
			}
		}

		private void HandleTouchCanceled(Touch touch)
		{
			_initialTouchPositions.Remove(touch.fingerId);
			_maximumOffsetPosition.Remove(touch.fingerId);
		}

		private void HandleTouchEnded(Touch touch, ref bool shouldShowConsole)
		{
			if (_initialTouchPositions.ContainsKey(touch.fingerId))
			{
				Vector2 val = _initialTouchPositions[touch.fingerId];
				if (!_maximumOffsetPosition.TryGetValue(touch.fingerId, out var value))
				{
					value = val;
				}
				Vector2 offset = value - val;
				float magnitude = offset.magnitude;
				var isSwipeDown = DetectSwipeDown(magnitude, offset, ref shouldShowConsole);
                var isSwipeUp = DetectSwipeUp(magnitude, offset);
				if (!isSwipeDown && !isSwipeUp)
                {
                    DetectHold(UnityEngine.Time.realtimeSinceStartup - _touchStartTime, ref shouldShowConsole);
                }
				_initialTouchPositions.Remove(touch.fingerId);
				_maximumOffsetPosition.Remove(touch.fingerId);
			}
		}

		private bool DetectSwipeUp(float maxDistanceMoved, Vector2 offset)
		{
			if (maxDistanceMoved > 180f && Vector2.Dot(offset.normalized, Vector2.up) > 0.85f && UnityEngine.Input.touchCount == _fingersCount)
			{
                DebugConsole.IsVisible = false;
				return true;
			}
			return false;
		}

		private bool DetectSwipeDown(float maxDistanceMoved, Vector2 offset, ref bool shouldShowConsole)
		{
			if (maxDistanceMoved > 180f && Vector2.Dot(offset.normalized, -Vector2.up) > 0.85f && UnityEngine.Input.touchCount == _fingersCount)
			{
				shouldShowConsole = true;
                return true;
            }
            return false;
        }

        private bool DetectHold(float timeElapsed, ref bool shouldShowConsole)
        {
            if (timeElapsed > 1f && UnityEngine.Input.touchCount == _fingersCount)
            {
                shouldShowConsole = true;
                return true;
            }
            return false;
        }
    }
}