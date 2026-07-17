using Teabag.Player;
using UnityEngine;

public interface ITwoHandGrabbable
{
    Grabber SecondaryGrabber { get; }
    Transform PrimaryHandPosition { get; }
    Transform SecondaryHandPosition { get; }
    bool IsAbleUseTwoHand { get; }
    public bool IsTwoHandMode { get; }
    void SetSecondaryGrabber(Grabber grabber);
}
