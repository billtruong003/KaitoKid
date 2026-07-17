using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class TestTab : IComputerTab
{
    public static bool testMode;

    public override void OnClose()
    {

    }

    public override void OnOpen()
    {
        Render();
    }

    public override void Press(KeyCode key)
    {
        if (key == KeyCode.Return)
        {
            testMode = !testMode;
            Render();
        }
    }

    void Render()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Test Mode will display basic information on your screen.");
        builder.AppendLine();
        builder.AppendLine(testMode ? "Test Mode is enabled" : "Test mode is disabled");
        builder.AppendLine();
        builder.AppendLine("Press enter to enable or disable.");
        computer.RenderText(builder.ToString());
    }
}
