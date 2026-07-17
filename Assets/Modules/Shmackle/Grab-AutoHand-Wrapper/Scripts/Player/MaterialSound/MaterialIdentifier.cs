using TMPro;
using UnityEngine;
using Utils.Bill.InspectorCustom;
namespace Shmackle.SoundMaterial
{
    public class MaterialIdentifier : MonoBehaviour
    {
        public MaterialType materialType = MaterialType.Default;
        public MaterialSoundSystem materialSoundSystem;

        [CustomButton("Init Text")]
        public void InitText()
        {
            TextMeshPro textIdentify = GetComponentInChildren<TextMeshPro>();
            if (textIdentify != null)
            {
                textIdentify.text = materialType.ToString();
            }
            else
            {
                Debug.LogWarning("TextMeshPro component not found in children.");
            }
        }
    }

    public enum MaterialType
    {
        Default,
        Wood,
        Metal,
        Stone,
        Glass,
        Fabric,
        Dirt,
        Grass,
        Sand,
        WetLeaves,
        Snow,
    }
}