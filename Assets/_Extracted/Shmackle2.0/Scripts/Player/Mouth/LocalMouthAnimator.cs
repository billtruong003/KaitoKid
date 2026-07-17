using UnityEngine;

namespace Shmackle.Player.Mouth
{
    public class LocalMouthAnimator : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer _meshRenderer;
        private const string MOUTH_SHAPE_NAME = "Talking";
        private int _blendShapeIndex;

        private void Awake()
        {
            if (!_meshRenderer)
                _meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

            if (_meshRenderer)
                _blendShapeIndex = _meshRenderer.sharedMesh.GetBlendShapeIndex(MOUTH_SHAPE_NAME);
        }


        public void SetMouthValue(float value) { _meshRenderer.SetBlendShapeWeight(_blendShapeIndex, value); }
    }
}