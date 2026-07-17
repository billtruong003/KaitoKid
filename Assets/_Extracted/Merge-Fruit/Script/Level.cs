using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FruitCasual
{
    public class Level : MonoBehaviour
    {
        [SerializeField] public int LevelObject;
        [SerializeField] private GameObject upgradeObject;
        [SerializeField] public bool hasMerged = true;

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (LevelObject == 11 || upgradeObject == null)
                return;
            if (col.gameObject.layer != gameObject.layer && !hasMerged)
                return;
            hasMerged = true;

            MainManager.Instance.UpgradeObject(transform.gameObject, col.gameObject, upgradeObject);
            if (LevelObject < 6)
            {
                MainManager.Instance.AddToPoolingObjects(gameObject);
                Debug.Log($"AddToPoolingObject: {gameObject.name}");
            }

            gameObject.SetActive(false);
        }
        private bool CheckContains(GameObject obj)
        {
            int upgradeIndex = obj.name.IndexOf("UpgradeObject");

            if (upgradeIndex != -1)
            {
                // Nếu chứa, loại bỏ phần "UpgradeObject" từ tên
                return true;
            }
            return false;
        }

    }
}
