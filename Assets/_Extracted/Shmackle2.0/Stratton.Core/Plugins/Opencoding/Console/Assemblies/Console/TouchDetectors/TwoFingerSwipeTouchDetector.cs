using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace Opencoding.Console.TouchDetectors
{
	// This detects two fingers swiped down/up the screen to open/close the console.
	class TwoFingerSwipeTouchDetector : TouchDetector
	{
		private readonly Dictionary<int, Vector2> _initialTouchPositions = new Dictionary<int, Vector2>();
		private readonly Dictionary<int, Vector2> _maximumOffsetPosition = new Dictionary<int, Vector2>();

		private const float DRAG_DISTANCE_REQUIRED = 180.0f;

#if ENABLE_INPUT_SYSTEM
		public TwoFingerSwipeTouchDetector()
		{
			EnhancedTouchSupport.Enable();
		}
#endif

		public bool Update()
		{
			var shouldShowConsole = false;
#if ENABLE_INPUT_SYSTEM			
			foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
			{
				switch (touch.phase)
				{
					case UnityEngine.InputSystem.TouchPhase.Began:
						HandleTouchBegan(touch.finger.index, touch.screenPosition);
						break;
					case UnityEngine.InputSystem.TouchPhase.Moved:
						if (!_initialTouchPositions.ContainsKey(touch.finger.index))
							goto case UnityEngine.InputSystem.TouchPhase.Began;
						HandleTouchMoved(touch.finger.index, touch.screenPosition);
						break;
					case UnityEngine.InputSystem.TouchPhase.Ended:
						HandleTouchEnded(touch.finger.index, ref shouldShowConsole);
						break;
					case UnityEngine.InputSystem.TouchPhase.Canceled:
						HandleTouchCanceled(touch.finger.index);
						break;
				}
			}
#else
			foreach (var touch in Input.touches)
			{
				switch (touch.phase)
				{
					case TouchPhase.Began:
						HandleTouchBegan(touch.fingerId, touch.position);
						break;
					case TouchPhase.Moved:
						if (!_initialTouchPositions.ContainsKey(touch.fingerId))
							goto case TouchPhase.Began;
						HandleTouchMoved(touch.fingerId, touch.position);
						break;
					case TouchPhase.Ended:
						HandleTouchEnded(touch.fingerId, ref shouldShowConsole);
						break;
					case TouchPhase.Canceled:
						HandleTouchCanceled(touch.fingerId);
						break;
				}
			}
#endif
			return shouldShowConsole;
		}

		private void HandleTouchBegan(int finger, Vector2 position)
		{
			_maximumOffsetPosition.Remove(finger);
			_initialTouchPositions[finger] = position;
		}

		private void HandleTouchMoved(int finger, Vector2 position)
		{
			Vector2 initialPosition = _initialTouchPositions[finger];
			Vector2 maxOffsetPos;
			if (!_maximumOffsetPosition.TryGetValue(finger, out maxOffsetPos))
			{
				maxOffsetPos = initialPosition;
			}

			float oldDistanceMoved = Vector2.Distance(initialPosition, maxOffsetPos);
			float newDistance = Vector2.Distance(initialPosition, position);

			if (newDistance > oldDistanceMoved)
			{
				_maximumOffsetPosition[finger] = position;
			}
		}

		private void HandleTouchCanceled(int finger)
		{
			_initialTouchPositions.Remove(finger);
			_maximumOffsetPosition.Remove(finger);
		}

		private void HandleTouchEnded(int finger, ref bool shouldShowConsole)
		{
			if (!_initialTouchPositions.ContainsKey(finger))
				return;

			Vector2 initialPosition = _initialTouchPositions[finger];
			Vector2 maxOffsetPos;
			if (!_maximumOffsetPosition.TryGetValue(finger, out maxOffsetPos))
			{
				maxOffsetPos = initialPosition;
			}

			Vector2 offset = maxOffsetPos - initialPosition;
			float maxDistanceMoved = offset.magnitude;

			DetectSwipeDown(maxDistanceMoved, offset, ref shouldShowConsole);
			DetectSwipeUp(maxDistanceMoved, offset);

			_initialTouchPositions.Remove(finger);
			_maximumOffsetPosition.Remove(finger);
		}

		private void DetectSwipeUp(float maxDistanceMoved, Vector2 offset)
		{
			if (maxDistanceMoved > DRAG_DISTANCE_REQUIRED &&
			    Vector2.Dot(offset.normalized, Vector2.up) > 0.85f && 
			    _maximumOffsetPosition.Count == 2)
			{
				DebugConsole.IsVisible = false;
			}
		}

		private void DetectSwipeDown(float maxDistanceMoved, Vector2 offset, ref bool shouldShowConsole)
		{
			if (maxDistanceMoved > DRAG_DISTANCE_REQUIRED &&
			    Vector2.Dot(offset.normalized, -Vector2.up) > 0.85f && 
			    _maximumOffsetPosition.Count == 2)
			{
				shouldShowConsole = true;
			}
		}
	}
}