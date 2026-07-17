using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Teabag.UI;

public class MuteButton : GorillaButton
{
    public PlayerLine line;

    public override void OnPress()
    {
        line.gorilla.isMuted = !line.gorilla.isMuted;
        SetMaterial(line.gorilla.isMuted);
    }

    public async UniTaskVoid OnEnable()
    {
        visual = false;
        while (line.gorilla == null)
            await UniTask.Yield();

        while (string.IsNullOrEmpty(line.gorilla.id))
            await UniTask.Yield();

        SetMaterial(line.gorilla.isMuted);
    }
}
