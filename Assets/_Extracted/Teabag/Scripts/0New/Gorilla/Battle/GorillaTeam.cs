using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Core;
using Teabag.Player;

namespace Teabag.Player
{
public class GorillaTeam : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnTeamChanged))]
    public int team { get; set; }
    
    public Color colour
    {
        get
        {
            return GameServices.GetTeamColour?.Invoke(team) ?? Color.white;
        }
    }
    
    public List<Renderer> visualisers = new List<Renderer>();

    Gorilla gorilla;

    Action<object> teamSwitchedWrapper;

    private MaterialPropertyBlock _mpb;
    private static readonly int ColorProp = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        gorilla = GetComponent<Gorilla>();
        teamSwitchedWrapper = (obj) => TeamSwitchedCallback(obj as Gorilla);
        GameServices.OnTeamSwitched += teamSwitchedWrapper;
    }

    private void OnDestroy()
    {
        GameServices.OnTeamSwitched -= teamSwitchedWrapper;
    }

    public override void Spawned()
    {
        base.Spawned();
        OnTeamChanged();
    }

    public void TeamSwitchedCallback(Gorilla switcher)
    {
        // Avoiding checking which gorilla it is - just in case of desync

        foreach (Renderer visualiserRenderer in visualisers)
        {
            visualiserRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorProp, colour);
            visualiserRenderer.SetPropertyBlock(_mpb);

            if (visualiserRenderer.sharedMaterial != null && visualiserRenderer.sharedMaterial.name.Contains("Beam"))
                visualiserRenderer.gameObject.SetActive((GameServices.SharesTeam?.Invoke(gorilla) ?? false) && !gorilla.HasStateAuthority);
        }
    }


    public void OnTeamChanged()
    {
        try
        {
            GameServices.OnTeamSwitched?.Invoke(gorilla);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to call team switch: " + e);
        }
    }
}
}
