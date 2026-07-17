using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Unity.VisualScripting;

namespace FruitCasual
{
    // Class responsible for initializing Prefab objects
    public class InitPrefab : MonoBehaviour
    {
        // Variables displayed in the Unity editor
        [SerializeField] private MergeableConfig mergeObjectConfig;
        [SerializeField] private Utils utilities;
        [SerializeField] private GameObject sampleObject;
        [SerializeField] private List<GameObject> objects = new List<GameObject>();
        [SerializeField] private SkinCode skinCode;
        private float initialValue = 0.3f;
        private float multiplicationFactor = 1.4f;
        private float sizeValue = 0;

        // Method called when the "Setup" button is pressed in the Unity editor
        [Button("Setup")]
        private void Setup()
        {
            initialValue = 0.5f;
            int count = 0;
            Clear(); // Call the Clear method to remove current objects

            int multipleTimes = mergeObjectConfig.fruitObjects.Count;

            // Iterate through the list of objects in mergeObjectConfig
            for (int i = 0; i < multipleTimes; i++)
            {
                if (skinCode == mergeObjectConfig.fruitObjects[i].code)
                {
                    foreach (SkinFruit skinFruit in mergeObjectConfig.fruitObjects[i].skinFruits)
                    {
                        count++;
                        Debug.Log(skinFruit);
                        // Instantiate a new object for each skinFruit
                        GameObject objectFruit = Instantiate(sampleObject, Vector3.zero, Quaternion.identity);
                        Level _level = objectFruit.AddComponent<Level>();
                        Fruit _fruit = objectFruit.AddComponent<Fruit>();

                        // Set level and objectLevel properties
                        _level.LevelObject = count;
                        _fruit.objectLevel = utilities.GetLevelObjectByNum(count);
                        objects.Add(objectFruit);

                        // Update size and scale of the object
                        initialValue *= multiplicationFactor;
                        sizeValue = initialValue;
                        _fruit.size = sizeValue;
                        objectFruit.transform.localScale = new Vector3(sizeValue, sizeValue, sizeValue);

                        objectFruit.name = $"FruitLevel {count}";

                        // Instantiate the fruitPrefab as a child object
                        GameObject fruitPrefab = skinFruit.objectPrefab;
                        GameObject spriteObject = Instantiate(fruitPrefab, objectFruit.transform);
                        spriteObject.name = "Fruit Sprite";
                        spriteObject.transform.localPosition = Vector2.zero;
                    }
                }
            }
        }

        // Method called when the "Clear" button is pressed in the Unity editor
        [Button("Clear")]
        private void Clear()
        {
            // Remove existing objects from the list and destroy them
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                DestroyImmediate(objects[i]);
                objects.RemoveAt(i);
            }
        }

        // Generate a random color value
        private int randomColor()
        {
            int colNum = Random.Range(0, 256);
            return colNum;
        }

    }
}
