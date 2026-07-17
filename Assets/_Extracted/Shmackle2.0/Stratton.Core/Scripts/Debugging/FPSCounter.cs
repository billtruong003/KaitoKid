using UnityEngine;
using UnityEngine.UI;

namespace Stratton.Debugging
{
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField]
        private Text text_FPS;
        private float deltaTime = 0.0f;


        private void Update()
        {
            deltaTime += (UnityEngine.Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            text_FPS.text = "FPS: " + Mathf.Round(fps);
        }
    }
}
