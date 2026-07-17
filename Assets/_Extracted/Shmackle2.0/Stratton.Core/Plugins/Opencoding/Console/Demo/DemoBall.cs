using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Random = UnityEngine.Random;

namespace Opencoding.Console.Demo
{
	class DemoBall : MonoBehaviour
	{
		private BallGameController _ballGameController = null;

		[SerializeField] private Color[] _ballColors = null;

		public int BallColorIndex
		{
			get; private set;
		}

		public void Setup(BallGameController ballGameController)
		{
			_ballGameController = ballGameController;

			BallColorIndex = Random.Range(0, _ballColors.Length);
			GetComponent<SpriteRenderer>().color = _ballColors[BallColorIndex];

			ballGameController.RegisterBall(this);
		}

#if ENABLE_INPUT_SYSTEM
		void Update()
		{
			if (Pointer.current.press.wasReleasedThisFrame)
			{
				Vector2 pointerPos = Camera.main.ScreenToWorldPoint(Pointer.current.position.ReadValue());
				RaycastHit2D hit = Physics2D.Raycast(pointerPos, Vector2.zero);
				if (hit && hit.collider.gameObject == gameObject)
				{
					_ballGameController.DestroyAdjacentMatchingBalls(this);
				}
			}
		}
#else
		private void OnMouseUp()
		{
			_ballGameController.DestroyAdjacentMatchingBalls(this);
		}
#endif

		private void OnDestroy()
		{
			if (_ballGameController != null)
				_ballGameController.BallCollected(this);
		}
	}
}
