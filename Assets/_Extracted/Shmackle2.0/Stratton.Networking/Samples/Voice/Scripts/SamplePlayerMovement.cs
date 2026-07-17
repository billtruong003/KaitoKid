using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Stratton.Networking.Voice.Sample
{
    public class SamplePlayerMovement : NetworkBehaviour
    {
        private CharacterController _controller;

        [SerializeField]
        float _playerSpeed = 2f;

        private Vector2 _moveInput;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        public override void FixedUpdateNetwork()
        {
            Vector3 move = new Vector3(_moveInput.x, 0, _moveInput.y) * Runner.DeltaTime * _playerSpeed;
            _controller.Move(move);
        }
    }
}