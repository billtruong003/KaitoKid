using System.Text;
using GorillaLocomotion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;

public class TurnTab : IComputerTab
{
    private const string TURNING_KEY = "JungleXRKit.TurnMode";


    private IDataPersistenceService _persistence;


    public override void OnClose()
    {
        Refresh();
    }

    public override void OnOpen()
    {
        Refresh();
    }

    public override void Press(KeyCode key)
    {
        if (_persistence == null) return;

        switch (key)
        {
            case KeyCode.Keypad1:
                _persistence.TrySaveData(TURNING_KEY, 0);
                break;
            case KeyCode.Keypad2:
                _persistence.TrySaveData(TURNING_KEY, 1);
                break;
            case KeyCode.Keypad3:
                _persistence.TrySaveData(TURNING_KEY, 2);
                break;
            default:
                break;
        }
        Refresh();
    }

    public void Refresh()
    {
        if (_persistence == null)
        {
            _persistence = ServiceLocator.Get<IDataPersistenceService>();
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("1: No Turn");
        builder.AppendLine("2: Snap Turn");
        builder.AppendLine("3: Smooth Turn");
        builder.AppendLine();
        builder.AppendLine("Turn option: " + (GorillaTurning.TurnState)(_persistence?.LoadData<int>(TURNING_KEY, 0) ?? 0));

        int turnMode = 0;

        turnMode = _persistence?.LoadData<int>(TURNING_KEY, 1) ?? 0;
        HandleOnClick_ChangeRotateMode(turnMode);

        computer.RenderText(builder.ToString());
    }

    public void HandleOnClick_ChangeRotateMode(int mode)
    {
        var rig = LocalHardwareRig;
        if (rig == null) return;

        if (!TryGetTurnModule(rig, out var turnModule)) return;

        turnModule.TurnMode = (TurnModeType)mode;
    }

    private bool TryGetTurnModule(IHardwareRig rig, out TurnLocomotion turnModule)
    {
        turnModule = null;
        var locomotionController = rig.LocomotionController;
        if (locomotionController == null) return false;

        locomotionController.GetLocomotionModule(out turnModule);
        return turnModule != null;
    }
}
