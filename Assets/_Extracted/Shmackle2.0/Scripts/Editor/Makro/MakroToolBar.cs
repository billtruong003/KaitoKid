using System; 
using System.Collections.Generic; 
using System.Text; 
using UnityEngine; 
using UnityEditor;

namespace Stratton.Core.Editor
{

public class MakroToolBar
{
[MenuItem("Makro/Open Main")]
public static void Makro_Open_Main()
{
var makroData = MakroToolBarGenerator.LoadMakroData(0);
MakroWindowController.UseMakroData(makroData);

}
[MenuItem("Makro/Play Main")]
public static void Makro_Play_Main()
{
var makroData = MakroToolBarGenerator.LoadMakroData(1);
MakroWindowController.UseMakroData(makroData);

}
[MenuItem("Makro/Open Gameplay")]
public static void Makro_Open_Gameplay()
{
var makroData = MakroToolBarGenerator.LoadMakroData(2);
MakroWindowController.UseMakroData(makroData);

}
[MenuItem("Makro/Open UIHUD")]
public static void Makro_Open_UIHUD()
{
var makroData = MakroToolBarGenerator.LoadMakroData(3);
MakroWindowController.UseMakroData(makroData);

}
[MenuItem("Makro/Refresh")]
public static void Makro_Refresh()
{
MakroToolBarGenerator.Refresh();

}

}
}
