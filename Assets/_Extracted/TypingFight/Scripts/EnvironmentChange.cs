using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using NaughtyAttributes;
namespace Environment
{
    public class EnvironmentChange : MonoBehaviour
    {
        [SerializeField] private EnvironmentConfig envConfig;
        [Header("Sky")]
        [SerializeField] private GameObject _sky;
        [SerializeField] private GameObject _sun;
        [SerializeField] private GameObject _clouds;
        [Header("Ground")]
        [SerializeField] private GameObject _ground;
        [Header("Objects")]
        [SerializeField] private GameObject _grass;
        [SerializeField] private GameObject _flower;
        [SerializeField] private GameObject _leaf;
        [SerializeField] private GameObject _trees;
        [SerializeField] private GameObject _rocks;
        [SerializeField] private string _currentEnvironment;

        [Header("CheatEnvironment")]
        [SerializeField] private bool ForcedEnvironment = false;
        [SerializeField] private string envForce;
        private List<string> environments = new List<string>()
        {
            "rock",
            "snow",
            "desert",
            "dark",
        };
        [Button("Test")]
        private void Awake()
        {
            int index = UnityEngine.Random.Range(0, environments.Count);
            _currentEnvironment = environments[index];
            if (ForcedEnvironment)
                _currentEnvironment = envForce;

            Debug.Log(_currentEnvironment);
            InitEnvironment(_currentEnvironment);
        }
        private void InitEnvironment(string currentEnvironment)
        {
            int environmentIndex = envConfig.GetSpritesIndex(currentEnvironment);
            Debug.Log(environmentIndex);
            EnvironmentConfig.EnvironmentData envSprite = envConfig.SpritesData[environmentIndex];

            // Sky
            if (envSprite.EnvironmentType == EnvironmentConfig.Environment.NIGHT)
            {
                SpriteRenderer skyImage = _sky.GetComponent<SpriteRenderer>();
                skyImage.color = envConfig.DayCol;
            }
            else
            {
                SpriteRenderer skyImage = _sky.GetComponent<SpriteRenderer>();
                skyImage.color = envConfig.DayCol;
            }

            // Foreground
            SpriteRenderer foreGround = _ground.GetComponent<SpriteRenderer>();
            foreGround.sprite = envSprite.Sprites.GetGround();

            // Object
            GrassUpdate(envSprite);
            FlowerUpdate(envSprite);
            LeafUpdate(envSprite);
            TreeUpdate(envSprite);
        }
        private void GrassUpdate(EnvironmentConfig.EnvironmentData envSprite)
        {
            Transform grass1 = _grass.transform.GetChild(0);
            Transform grass2 = _grass.transform.GetChild(1);
            foreach (Transform child in grass1)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                renderer.sprite = envSprite.Sprites.GetGrass1();
            }
            foreach (Transform child in grass2)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                renderer.sprite = envSprite.Sprites.GetGrass2();
            }
        }
        private void FlowerUpdate(EnvironmentConfig.EnvironmentData envSprite)
        {
            foreach (Transform child in _flower.transform)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                renderer.sprite = envSprite.Sprites.GetFlower();
            }
        }
        private void LeafUpdate(EnvironmentConfig.EnvironmentData envSprite)
        {
            foreach (Transform child in _leaf.transform)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                renderer.sprite = envSprite.Sprites.GetLeaf();
            }
        }
        private void TreeUpdate(EnvironmentConfig.EnvironmentData envSprite)
        {
            foreach (Transform child in _trees.transform)
            {
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                renderer.sprite = envSprite.Sprites.GetTrees();
            }
        }
    }
}
