using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.EnhancedTouch;
#endif

namespace Opencoding.Console.TouchDetectors
{
	// This detects three fingers held on the screen to open/close the console
	class ThreeFingersHeldTouchDetector : TouchDetector
	{
		private readonly Dictionary<int, Vector2> _initialTouchPositions = new Dictionary<int, Vector2>();
		private bool _touchCancelled = false;
		private float _timeToActivateConsole = -1;
		
		private const float TIME_TO_HOLD_FINGERS_BEFORE_ACTIVATING = 0.6f;
		private const float MAX_MOVEMENT_ALLOWED = 20.0f;

#if ENABLE_INPUT_SYSTEM
		public ThreeFingersHeldTouchDetector()
		{
			EnhancedTouchSupport.Enable();
		}
#endif
		
		public bool Update()
		{
			int touchCount = 0;
#if ENABLE_INPUT_SYSTEM
			touchCount = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count;
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
						HandleTouchEnded();
						break;
					case UnityEngine.InputSystem.TouchPhase.Canceled:
						HandleTouchCanceled();
						break;
				}
			}
#else
			touchCount = Input.touchCount;
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
						HandleTouchEnded();
						break;
					case TouchPhase.Canceled:
						HandleTouchCanceled();
						break;
				}
			}
#endif
			if (_timeToActivateConsole < 0 && !_touchCancelled)
			{
				if (touchCount == 3)
				{
					_timeToActivateConsole = Time.realtimeSinceStartup + TIME_TO_HOLD_FINGERS_BEFORE_ACTIVATING;
				}
				else
				{
					_timeToActivateConsole = -1;
				}
			}
			else if (_timeToActivateConsole > 0 && Time.realtimeSinceStartup > _timeToActivateConsole && !_touchCancelled)
			{
				return true;
			}
			return false;
		}

		private void HandleTouchBegan(int finger, Vector2 position)
		{
			// First touch, reset things
			if (finger == 0)
			{
				_touchCancelled = false;
				_timeToActivateConsole = -1;
			}

			_initialTouchPositions[finger] = position;
		}
		
		private void HandleTouchMoved(int finger, Vector2 position)
		{
			Vector2 initialPosition = _initialTouchPositions[finger];
			if (Vector2.Distance(position, initialPosition) > MAX_MOVEMENT_ALLOWED)
			{			
				_touchCancelled = true;
			}
		}
		
		private void HandleTouchCanceled()
		{
			_touchCancelled = true;
		}
		
		private void HandleTouchEnded()
		{
			_touchCancelled = true;
		}
	}
}