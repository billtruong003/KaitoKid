using UnityEngine;

namespace GameSystem.Player
{
    [System.Serializable]
    public class PlayerCamera
    {
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private Transform orientation;
        [SerializeField] private Transform playerBody;
        [SerializeField] private float sensitivity = 2f;

        private float xRotation;
        private float yRotation;
        private float baseYaw; // yaw gốc khi ngồi, dùng để clamp ±limit

        // Giới hạn động — StateMachine set mỗi frame
        private float currentYawLimit = 360f;
        private float currentPitchLimit = 85f;

        public Transform CameraTransform => cameraHolder;

        public void Initialize()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Gọi khi chuyển mode để cập nhật giới hạn camera.
        /// baseYaw được snapshot lại mỗi khi vào Seated/Peephole.
        /// </summary>
        public void SetLimits(float yawLimit, float pitchLimit, bool snapBaseYaw = false)
        {
            currentYawLimit = yawLimit;
            currentPitchLimit = pitchLimit;
            if (snapBaseYaw)
            {
                baseYaw = yRotation;
            }
        }

        public void HandleLook()
        {
            float mouseX = Input.GetAxisRaw("Mouse X") * sensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivity;

            yRotation += mouseX;
            xRotation -= mouseY;

            // Pitch clamp
            xRotation = Mathf.Clamp(xRotation, -currentPitchLimit, currentPitchLimit);

            // Yaw clamp (nếu < 360 → giới hạn quanh baseYaw)
            if (currentYawLimit < 360f)
            {
                float minYaw = baseYaw - currentYawLimit;
                float maxYaw = baseYaw + currentYawLimit;
                yRotation = Mathf.Clamp(yRotation, minYaw, maxYaw);
            }

            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
            playerBody.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }
}
