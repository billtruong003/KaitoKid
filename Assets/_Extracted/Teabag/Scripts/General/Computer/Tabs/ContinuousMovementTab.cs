using System.Text;
using GorillaLocomotion;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using UnityEngine;
using Teabag.Core;

public class ContinuousMovementTab : IComputerTab
{
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
                _persistence.TrySaveData("ContinuosMovement", 0);
                break;
            case KeyCode.Keypad2:
                _persistence.TrySaveData("ContinuosMovement", 1);
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
        builder.AppendLine("1: Turn On");
        builder.AppendLine("2: Turn Off");
        builder.AppendLine();
        builder.AppendLine("Joystick movement option: " + ((_persistence?.LoadData<int>("ContinuosMovement", 0) ?? 0) == 0 ? "On" : "Off"));

        HandleOnClick_ChangeContinuosMode((_persistence?.LoadData<int>("ContinuosMovement", 0) ?? 0) == 0);

        computer.RenderText(builder.ToString());
    }

    public void HandleOnClick_ChangeContinuosMode(bool isActive)
    {
        var rig = LocalHardwareRig;
        if (rig == null) return;

        if (!TryGetContinuousModule(rig, out var module)) return;

        module.Enabled = isActive;
    }

    private bool TryGetContinuousModule(IHardwareRig rig, out ContinuousMovement turnModule)
    {
        turnModule = null;
        var locomotionController = rig.LocomotionController;
        if (locomotionController == null) return false;

        locomotionController.GetLocomotionModule(out turnModule);
        return turnModule != null;
    }
}
