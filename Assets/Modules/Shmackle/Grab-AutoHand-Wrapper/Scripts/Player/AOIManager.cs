using System.Collections.Generic;
using Fusion;
using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// Manages Area of Interest (AOI) for all players in Photon Fusion Shared Mode.
/// </summary>
public class AOIManager : NetworkBehaviour, IInterestEnter, IInterestExit
{
    [HorizontalLine]
    [Header("AOI")]
    [Tooltip("The area of interest radius used for players.")]
    public int AreaOfInterestRadius = 32;
    

    

    
    public override void FixedUpdateNetwork()
    {
        if ((Runner.IsServer || Object.HasStateAuthority) && !Object.InputAuthority.IsNone)
        {
            // The player interest must be cleared when no in share mode.
            if (Runner.GameMode != GameMode.Shared)
                Runner.ClearPlayerAreaOfInterest(Object.InputAuthority);
            Runner.AddPlayerAreaOfInterest(Object.InputAuthority, transform.position, AreaOfInterestRadius);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, AreaOfInterestRadius);
    }
#endif
    
    
    public void InterestEnter(PlayerRef player)
    {
        if (Runner.LocalPlayer != player || !Runner.GetVisible())
            return;

        Debug.Log("Interest Enter");
        gameObject.SetActive(true);
    }

    public void InterestExit(PlayerRef player)
    {
        if (Runner.LocalPlayer != player || !Runner.GetVisible())
            return;

        Debug.Log("Interest Exit");
        gameObject.SetActive(false);
    }
}
