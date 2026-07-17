using UnityEngine;
using UnityEngine.UI;

namespace Opencoding.Console.Demo
{
	class BallGameHUD : MonoBehaviour
	{
		[SerializeField] private Text _levelLabel = null;
		[SerializeField] private Text _turnsLabel = null;
		[SerializeField] private Text _targetLabel = null;

		private BallGameController _ballGameController;
		
		public void Setup(BallGameController ballGameController)
		{
			_ballGameController = ballGameController;
		}

		private void Update()
		{
			if (_ballGameController == null)
				return;

			_turnsLabel.text = string.Format("Turns left: {0}", _ballGameController.TurnsLeft);
			_levelLabel.text = string.Format("Level: {0}", _ballGameController.Level);
			_targetLabel.text = string.Format("Target: {0}/{1}", _ballGameController.BallsDestroyedThisLevel, _ballGameController.BallTargetThisLevel);
		}
	}
}
