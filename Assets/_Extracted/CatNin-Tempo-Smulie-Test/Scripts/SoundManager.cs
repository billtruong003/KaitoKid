using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;
    [SerializeField] private List<AudioClip> soundTracks;
    [SerializeField] private List<AudioClip> sfxs;
    [SerializeField] private AudioSource soundTrack;
    [SerializeField] private AudioSource backgroundMusic;
    [SerializeField] private AudioSource sfx;
    [SerializeField] private bool isPersistent;

    private void Awake()
    {
        if (isPersistent)
        {
            DontDestroyOnLoad(this.gameObject);
        }
        Instance = this;
    }
    private void OnDestroy()
    {
        Instance = null;
    }
    public void PlayMusic(int musicNum)
    {
        soundTrack.PlayOneShot(soundTracks[musicNum]);
    }
    public void ShurikenThrow()
    {
        sfx.PlayOneShot(sfxs[0]);
    }
    public void MashWood()
    {
        sfx.PlayOneShot(sfxs[1]);
    }
    public void StopAllAudio()
    {
        soundTrack.Stop();
        backgroundMusic.Stop();
        sfx.Stop();
    }
}
