using Autohand;
using UnityEngine;
using UnityEngine.InputSystem;

public class AutoHandFingerBender : MonoBehaviour
{
    public Hand hand;
    public InputActionProperty bendAction;
    public InputActionProperty unbendAction;
    bool pressed;

    public void BendAction(float[] bendOffsets)
    {
        if (!pressed)
        {
            pressed = true;
            for (int i = 0; i < hand.fingers.Length; i++)
            {
                hand.fingers[i].bendOffset += bendOffsets[i];
            }
        }
    }

    public void UnbendAction(float[] bendOffsets)
    {
        if (pressed)
        {
            pressed = false;
            for (int i = 0; i < hand.fingers.Length; i++)
            {
                hand.fingers[i].bendOffset -= bendOffsets[i];
            }
        }
    }
}
