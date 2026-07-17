using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollManager : MonoBehaviour
{
    [SerializeField] private Renderer bgTop;
    [SerializeField] private Renderer bgMiddle;
    [SerializeField] private Renderer bgBot;
    [SerializeField] private Renderer bushRight;
    [SerializeField] private Renderer bushLeft;
    [SerializeField] private float limScrollBack = -11;

    void Update()
    {
        ScrollRenderer(bgTop, 0.7f);
        ScrollRenderer(bgMiddle, 0.14f);
        ScrollRenderer(bgBot, 0.0275f);
        ScrollRenderer(bushLeft, 0.7f);
        ScrollRenderer(bushRight, -0.7f);
    }
    private void ScrollRenderer(Renderer bgRender, float scrollSpeed)
    {
        bgRender.material.mainTextureOffset += new Vector2(0, scrollSpeed * Time.deltaTime);
    }
    private void ScrollBackground(Transform bg, float scrollSpeed)
    {
        float moveAmount = scrollSpeed * Time.deltaTime;
        bg.Translate(Vector3.down * moveAmount);

        if (bg.position.y <= limScrollBack)
        {
            bg.position = new Vector3(bg.position.x, 11f, bg.position.z);
        }
    }
}
