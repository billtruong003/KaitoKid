using UnityEngine;
using GameSystem.Audio;

namespace GameSystem.Environment
{
    public class SurfaceMaterial : MonoBehaviour
    {
        [SerializeField] private SurfaceType surfaceType;
        
        public SurfaceType SurfaceType => surfaceType;
    }
}