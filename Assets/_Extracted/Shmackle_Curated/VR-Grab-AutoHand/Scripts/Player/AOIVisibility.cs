using Fusion;
using UnityEngine;

public class AOIVisibility : NetworkBehaviour, IInterestEnter, IInterestExit
{
    public void InterestEnter(PlayerRef player)
    {
        if (Runner.LocalPlayer == player && Runner.GetVisible())
        {
            Debug.Log("player enter");
            gameObject.SetActive(true);
        }
    }

    public void InterestExit(PlayerRef player)
    {
        if (Runner.LocalPlayer == player && Runner.GetVisible())
        {
            gameObject.SetActive(false);
        }
    }

    // public override void Spawned()
    // {
    //     if (Object.IsProxy)
    //     {
    //         gameObject.SetActive(false);
    //     }
    // }
}