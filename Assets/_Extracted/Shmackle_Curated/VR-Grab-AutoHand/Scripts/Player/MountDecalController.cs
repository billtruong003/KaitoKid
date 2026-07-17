using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MountDecalController : MonoBehaviour
{
    [SerializeField] private float durationTime;
    [SerializeField] private string decalID;
    
    private void OnEnable()
    {
        DOVirtual.DelayedCall(durationTime, () =>
        {
            if (ShmackleKissDecalPooler.Instance == null)
            {
                return;
            }
            
            ShmackleKissDecalPooler.Instance.ReturnToPool(decalID, gameObject);
        });
    }
}
