using System;
using System.Text;
using Squido.JungleXRKit.Core;
using UnityEngine;

public class ColourTab : IComputerTab
{
    KeyCode editingColour = KeyCode.R;

    public override void OnClose()
    {

    }

    public override void OnOpen()
    {
        Render();
    }

    public override void Press(KeyCode key)
    {
        try
        {
            switch (key)
            {
                case KeyCode.R:
                    editingColour = KeyCode.R;
                    break;
                case KeyCode.G:
                    editingColour = KeyCode.G;
                    break;
                case KeyCode.B:
                    editingColour = KeyCode.B;
                    break;
                default:
                    string str = key.ToString();
                    if (str.StartsWith("Keypad"))
                    {
                        str = str.Replace("Keypad", "");
                        int num = int.Parse(str);
                        Color colour = GetColour();
                        switch (editingColour)
                        {
                            case KeyCode.R:
                                colour.r = num / 10f;
                                break;
                            case KeyCode.G:
                                colour.g = num / 10f;
                                break;
                            case KeyCode.B:
                                colour.b = num / 10f;
                                break;
                            default:
                                break;
                        }
                        SetColour(colour);
                    }
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        Render();
    }

    public void Render()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Press R to control red.");
        builder.AppendLine("Press G to control green.");
        builder.AppendLine("Press B to control blue.");

        builder.AppendLine();

        Color colour = GetColour();
        colour.r = Mathf.RoundToInt(colour.r * 10);
        colour.g = Mathf.RoundToInt(colour.g * 10);
        colour.b = Mathf.RoundToInt(colour.b * 10);

        string selected = " <--";

        builder.Append($"R: {colour.r}");
        builder.AppendLine(editingColour == KeyCode.R ? selected : "");
        builder.Append($"G: {colour.g}");
        builder.AppendLine(editingColour == KeyCode.G ? selected : "");
        builder.Append($"B: {colour.b}");
        builder.AppendLine(editingColour == KeyCode.B ? selected : "");

        computer.RenderText(builder.ToString());
    }

    public static void SetColour(Color colour)
    {
        var persistence = ServiceLocator.Get<IDataPersistenceService>();
        persistence.TrySaveData("R", colour.r);
        persistence.TrySaveData("G", colour.g);
        persistence.TrySaveData("B", colour.b);
    }

    public static Color GetColour()
    {
        var persistence = ServiceLocator.Get<IDataPersistenceService>();
        Color colour = new Color(0, 0, 0, 1);
        colour.r = persistence.LoadData<float>("R");
        colour.g = persistence.LoadData<float>("G");
        colour.b = persistence.LoadData<float>("B");
        return colour;
    }
}
