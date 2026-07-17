using UnityEngine;

namespace GameSystem.Player
{
    [System.Serializable]
    public class PlayerHeadbob
    {
        [SerializeField] private Transform cameraHolder;
        [SerializeField] private float bobFrequency = 2.5f;
        [SerializeField] private float bobAmplitude = 0.05f;
        [SerializeField] private float smoothTransition = 10f;

        private float phase;
        private Vector3 startPosition;
        private bool wasLowestPoint;

        public float CurrentPhase => phase;
        public bool IsLowestPoint { get; private set; }

        public void ProcessHeadbob(float velocity, bool isGrounded)
        {
            if (startPosition == Vector3.zero) startPosition = cameraHolder.localPosition;

            if (velocity > 0.1f && isGrounded)
            {
                phase += velocity * bobFrequency * Time.fixedDeltaTime;
                float sinValue = Mathf.Sin(phase);

                IsLowestPoint = sinValue < -0.95f && !wasLowestPoint;
                wasLowestPoint = sinValue < -0.95f;

                Vector3 targetPosition = startPosition + new Vector3(Mathf.Cos(phase / 2f) * bobAmplitude / 2f, sinValue * bobAmplitude, 0f);
                cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, targetPosition, Time.fixedDeltaTime * smoothTransition);
            }
            else
            {
                phase = 0f;
                cameraHolder.localPosition = Vector3.Lerp(cameraHolder.localPosition, startPosition, Time.fixedDeltaTime * smoothTransition);
                IsLowestPoint = false;
                wasLowestPoint = false;
            }
        }
    }
}