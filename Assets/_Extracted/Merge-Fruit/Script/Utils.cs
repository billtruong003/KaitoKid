using UnityEngine;


namespace FruitCasual
{
    public class Utils : MonoBehaviour
    {
        public ObjectLevel GetLevelObjectByNum(int enumNum)
        {
            if (ObjectLevel.IsDefined(typeof(ObjectLevel), enumNum))
            {
                ObjectLevel enumGet = (ObjectLevel)enumNum;
                Debug.Log("Enum set to: " + enumGet);
                return enumGet;
            }
            else
            {
                Debug.LogError("Invalid enum number: " + enumNum);
                return ObjectLevel.NONE;
            }
        }
    }
}