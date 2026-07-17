using Fusion;
using UnityEngine;

public enum QuestControllerInput
{
    Grip,
    Trigger,
    Primary
}

public class AutoHandFingerEvents : NetworkBehaviour
{
    #region ===== Fields =====

    [SerializeField]
    private Fusion.SerializableDictionary<QuestControllerInput, AutoHandFingerBender> autoHandFingerBenderMap = null;

    #endregion

    #region ===== Methods =====

    public void BendAction(QuestControllerInput input, float[] bendOffsets)
    {
        if (autoHandFingerBenderMap == null)
            return;
        if (autoHandFingerBenderMap.TryGetValue(input, out var bender))
            bender.BendAction(bendOffsets);
    }

    public void UnbendAction(QuestControllerInput input, float[] bendOffsets)
    {
        if (autoHandFingerBenderMap == null)
            return;
        if (autoHandFingerBenderMap.TryGetValue(input, out var bender))
            bender.UnbendAction(bendOffsets);
    }

    #endregion
}