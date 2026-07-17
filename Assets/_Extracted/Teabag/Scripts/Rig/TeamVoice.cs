using Teabag.Player.Rig;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Teabag.Core;
using Teabag.Player;

namespace Teabag.Player
{
[RequireComponent(typeof(AudioSource))]
public class TeamVoice : MonoBehaviour
{
    public VoiceData normal;
    public VoiceData team;
    public List<Behaviour> filters;
    AudioSource source;
    Gorilla gorilla;

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    public void Awake()
    {
        source = GetComponent<AudioSource>();
        gorilla = GetComponentInParent<Gorilla>();
    }

    public void Update()
    {
        bool hasTeamManager = GameServices.TeamManagerExists?.Invoke() ?? false;
        VoiceData use = !hasTeamManager ? normal : team;
        source.minDistance = use.minDistance;
        source.maxDistance = use.maxDistance;
        source.spatialBlend = 1;

        if (!hasTeamManager)
        {
            EnableFilters(false);
            return;
        }

        bool sharesTeam = SharesTeam();
        if (!sharesTeam)
        {
            EnableFilters(false);
            return;
        }
        var rig = LocalHardwareRig;
        if (rig == null) return;
        bool farAway = Vector3.Distance(transform.position, rig.LocomotionController.PlayerRigidbody.position) > 10;

        source.spatialBlend = farAway ? 0 : 1;
        EnableFilters(farAway);
    }

    public void EnableFilters(bool enable)
    {
        foreach (Behaviour behaviour in filters)
            behaviour.enabled = enable;
    }

    public bool SharesTeam()
    {
        if (gorilla == null)
            return false;

        return GameServices.SharesTeam?.Invoke(gorilla) ?? false;
    }

    [System.Serializable]
    public class VoiceData
    {
        public float minDistance;
        public float maxDistance;
    }
}
}
