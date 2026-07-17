using Teabag.Networking;
using Teabag.Player;
using System.Collections;
using System.Collections.Generic;
using Squido.JungleXRKit.Avatar;
using Squido.JungleXRKit.Core;
using Teabag.Core;
using UnityEngine;
using Teabag.Gameplay;

public class TutorialManager : MonoBehaviour
{
    public Recorder recorder;
    public List<TutorialSection> sections = new List<TutorialSection>();
    public List<AudioClip> targetHit = new List<AudioClip>();
    int hits = 0;
    List<int> hasPlayed = new List<int>();

    private IHardwareRig LocalHardwareRig
    {
        get
        {
            if (ServiceLocator.TryGet<IRigInfoService>(out var rigInfo))
                return rigInfo.HardwareRig;
            return null;
        }
    }

    public void OnHitTarget()
    {
        recorder.audioSource.clip = targetHit[Random.Range(0, targetHit.Count)];
        recorder.audioSource.Play();
        hits++;
        if (hits >= 3)
        {
            PlayAnimation(8);
            foreach (Grabbable grabbale in FindObjectsOfType<Grabbable>())
            {
                if (grabbale.Object.HasStateAuthority && grabbale is not Backpack)
                {
                    if (grabbale.grabber != null)
                        grabbale.grabber.Release();

                    grabbale.canGrab = false;
                }
            }
        }
    }

    private void Update()
    {
        var rig = LocalHardwareRig;
        if (rig == null) return;
        foreach (TutorialSection section in sections)
        {
            if (Vector3.Distance(rig.Headset.Position, section.point.position) < 1)
            {
                PlayAnimation(section.animationIndex);
            }
        }
    }

    void PlayAnimation(int animationIndex)
    {
        if (recorder.currentAnimation != animationIndex || !recorder.playing)
        {
            if (!hasPlayed.Contains(animationIndex))
            {
                recorder.PlayAnimation(animationIndex);
                hasPlayed.Add(animationIndex);
            }
        }
    }

    [System.Serializable]
    public struct TutorialSection
    {
        public Transform point;
        public int animationIndex;
    }
}
