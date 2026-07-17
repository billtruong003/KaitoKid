using UnityEngine;

namespace GameSystem.Player
{
    [System.Serializable]
    public class PlayerBlink
    {
        [SerializeField] private Material blinkMaterial;
        [SerializeField] private float blinkDuration = 0.15f;

        private float blinkTimer;
        private bool isBlinking;
        private float fromValue;
        private float toValue;

        public void Initialize()
        {
            blinkMaterial.SetFloat("_BlinkAmount", -1f);
        }

        /// <summary>
        /// Gọi để chạy blink 1 lần. from/to trong khoảng -1 đến 2.
        /// VD: BlinkOnce(2f, -1f) = mắt đang nhắm → mở ra
        ///     BlinkOnce(-1f, 2f) = mắt đang mở → nhắm lại
        /// </summary>
        public void BlinkOnce(float from, float to)
        {
            fromValue = from;
            toValue = to;
            blinkMaterial.SetFloat("_BlinkAmount", from);
            blinkTimer = 0f;
            isBlinking = true;
        }

        public void HandleBlinkLogic()
        {
            if (!isBlinking) return;

            blinkTimer += Time.deltaTime;
            float t = Mathf.Clamp01(blinkTimer / blinkDuration);
            float value = Mathf.Lerp(fromValue, toValue, t);
            blinkMaterial.SetFloat("_BlinkAmount", value);

            if (t >= 1f)
            {
                isBlinking = false;
            }
        }
    }
}