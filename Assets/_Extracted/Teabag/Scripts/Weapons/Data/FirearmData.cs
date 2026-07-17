using System.Collections.Generic;
using Teabag.Core;
using Teabag.Gameplay;
using UnityEngine;

/// <summary>
/// ScriptableObject holding configuration data for Firearm weapons.
/// Create one asset per weapon type (Pistol, Revolver, Shotgun, BananaGun, Sniper, Rifle).
/// </summary>
[CreateAssetMenu(fileName = "NewFirearmData", menuName = "Weapons/Firearm Data")]
public class FirearmData : ScriptableObject
{
    [Header("Firing")]
    public bool auto = false;
    public int msBetweenShots = 500;
    public int msReloadTime = 1000;
    public float recoil = 1;

    [Header("Bullet")]
    public int bulletsInShot = 1;
    public float bulletSpread = 5;
    public float speed = 4;
    public float range = 200;
    public float damageOverTime = 0;
    public string bulletTypeName = "PistolAmmo";
    public Bullet bulletPrefab;

    [Header("Damage Per Rarity")]
    public List<byte> damage = new List<byte>();

    [Header("Magazine")]
    public int magCapacity = 6;

    [Header("Audio")]
    public AdvancedAudioClip[] fireClip;
    public AdvancedAudioClip jammedClip;
    public AdvancedAudioClip[] reloadClip;
}
