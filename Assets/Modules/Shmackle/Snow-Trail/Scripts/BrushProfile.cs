using UnityEngine;

[CreateAssetMenu(fileName = "NewBrushProfile", menuName = "Shmackle/Snow/Brush Profile")]
public class BrushProfile : ScriptableObject
{
    [Tooltip("Grayscale texture defining the brush shape and falloff. White is full strength.")]
    public Texture2D BrushTexture;

    [Tooltip("Default radius of the brush in world units.")]
    [Min(0.1f)]
    public float Radius = 0.5f;

    [Tooltip("Default strength of the brush trail.")]
    [Range(0, 1)]
    public float Strength = 0.8f;
}