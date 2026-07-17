using UnityEngine;
using System;

namespace BillSamples.TowerDefense
{
    // ─── TOWER DATA ──────────────────────────────

    public enum TowerType { Arrow, Cannon, Ice, Lightning, Sniper, Poison }
    public enum TargetMode { First, Last, Strongest, Weakest, Nearest }

    [Serializable]
    public class TowerLevelData
    {
        public int cost;
        public float damage;
        public float attackSpeed; // seconds between attacks
        public float range;
        public float aoeRadius;  // 0 = single target
        public int chainCount;   // for lightning
        public float slowPercent; // for ice
        public float slowDuration;
        public float dotPerSecond; // for poison
        public float dotDuration;
        public int dotMaxStacks;
        public string bonusDesc;  // Lv3 special ability description
    }

    [Serializable]
    public class TowerDefinition
    {
        public TowerType type;
        public string displayName;
        public Color color;
        public TargetMode defaultTarget;
        public TowerLevelData[] levels; // [0]=base, [1]=lv2, [2]=lv3

        public int BaseCost => levels[0].cost;
        public int TotalCost
        {
            get
            {
                int total = 0;
                foreach (var l in levels) total += l.cost;
                return total;
            }
        }
    }

    // ─── ENEMY DATA ──────────────────────────────

    public enum EnemyType { Goblin, Orc, Wolf, Skeleton, ShieldOrc, Bat, DarkMage, Golem, Ghost, Dragon }

    [Serializable]
    public class EnemyDefinition
    {
        public EnemyType type;
        public string displayName;
        public Color color;
        public float baseHP;
        public float speed;
        public int armor;
        public int goldReward;
        public bool flying;          // ignores ground path
        public bool canResurrect;     // skeleton
        public int shieldHits;        // shield orc: blocks N hits
        public float healAuraRadius;  // dark mage
        public float healPerSecond;
        public float dodgeChance;     // ghost
        public bool immuneToSlow;     // golem
        public bool immuneToPoison;   // dragon
        public float attackDamage;    // dragon attacks towers
        public float attackInterval;
    }

    // ─── WAVE DATA ───────────────────────────────

    [Serializable]
    public class WaveEntry
    {
        public EnemyType enemyType;
        public int count;
    }

    [Serializable]
    public class WaveDefinition
    {
        public int waveNumber;
        public WaveEntry[] entries;
        public float spawnInterval = 0.8f;
        public int bonusGold = 15;
        public string hint; // Tutorial hint
    }

    // ─── STATIC DATABASE ─────────────────────────

    public static class TDDatabase
    {
        public static readonly TowerDefinition[] Towers = new[]
        {
            // ARROW
            new TowerDefinition
            {
                type = TowerType.Arrow, displayName = "Arrow Tower",
                color = new Color(0.3f, 0.6f, 0.2f), defaultTarget = TargetMode.First,
                levels = new[]
                {
                    new TowerLevelData { cost=80, damage=12, attackSpeed=0.8f, range=3.5f },
                    new TowerLevelData { cost=100, damage=20, attackSpeed=0.7f, range=3.8f },
                    new TowerLevelData { cost=180, damage=35, attackSpeed=0.5f, range=4.0f, bonusDesc="Double arrow" },
                }
            },
            // CANNON
            new TowerDefinition
            {
                type = TowerType.Cannon, displayName = "Cannon Tower",
                color = new Color(0.6f, 0.35f, 0.15f), defaultTarget = TargetMode.Nearest,
                levels = new[]
                {
                    new TowerLevelData { cost=120, damage=25, attackSpeed=1.5f, range=3.0f, aoeRadius=1.2f },
                    new TowerLevelData { cost=150, damage=40, attackSpeed=1.3f, range=3.0f, aoeRadius=1.5f },
                    new TowerLevelData { cost=250, damage=65, attackSpeed=1.1f, range=3.0f, aoeRadius=2.0f, bonusDesc="Stun 0.5s" },
                }
            },
            // ICE
            new TowerDefinition
            {
                type = TowerType.Ice, displayName = "Ice Tower",
                color = new Color(0.4f, 0.7f, 0.95f), defaultTarget = TargetMode.First,
                levels = new[]
                {
                    new TowerLevelData { cost=100, damage=8, attackSpeed=1.0f, range=3.0f, slowPercent=0.3f, slowDuration=2f },
                    new TowerLevelData { cost=120, damage=14, attackSpeed=1.0f, range=3.0f, slowPercent=0.4f, slowDuration=2.5f },
                    new TowerLevelData { cost=200, damage=22, attackSpeed=1.0f, range=3.0f, slowPercent=0.5f, slowDuration=3f, bonusDesc="15% freeze stun 1s" },
                }
            },
            // LIGHTNING
            new TowerDefinition
            {
                type = TowerType.Lightning, displayName = "Lightning Tower",
                color = new Color(0.9f, 0.85f, 0.2f), defaultTarget = TargetMode.Nearest,
                levels = new[]
                {
                    new TowerLevelData { cost=150, damage=18, attackSpeed=1.2f, range=4.0f, chainCount=3 },
                    new TowerLevelData { cost=180, damage=28, attackSpeed=1.2f, range=4.0f, chainCount=4 },
                    new TowerLevelData { cost=280, damage=42, attackSpeed=1.2f, range=4.0f, chainCount=6, bonusDesc="10% overcharge 3x" },
                }
            },
            // SNIPER
            new TowerDefinition
            {
                type = TowerType.Sniper, displayName = "Sniper Tower",
                color = new Color(0.5f, 0.2f, 0.2f), defaultTarget = TargetMode.Strongest,
                levels = new[]
                {
                    new TowerLevelData { cost=200, damage=45, attackSpeed=2.5f, range=6.0f },
                    new TowerLevelData { cost=250, damage=75, attackSpeed=2.2f, range=6.0f },
                    new TowerLevelData { cost=350, damage=120, attackSpeed=1.8f, range=6.0f, bonusDesc="Kill <15% HP" },
                }
            },
            // POISON
            new TowerDefinition
            {
                type = TowerType.Poison, displayName = "Poison Tower",
                color = new Color(0.3f, 0.7f, 0.25f), defaultTarget = TargetMode.Nearest,
                levels = new[]
                {
                    new TowerLevelData { cost=130, damage=5, attackSpeed=1.0f, range=3.0f, aoeRadius=2.0f, dotPerSecond=4, dotDuration=3, dotMaxStacks=3 },
                    new TowerLevelData { cost=160, damage=7, attackSpeed=1.0f, range=3.0f, aoeRadius=2.0f, dotPerSecond=7, dotDuration=3.5f, dotMaxStacks=3 },
                    new TowerLevelData { cost=250, damage=12, attackSpeed=1.0f, range=3.0f, aoeRadius=2.5f, dotPerSecond=12, dotDuration=4, dotMaxStacks=5, bonusDesc="Plague spread" },
                }
            },
        };

        public static readonly EnemyDefinition[] Enemies = new[]
        {
            new EnemyDefinition { type=EnemyType.Goblin, displayName="Goblin", color=new Color(0.3f,0.7f,0.2f), baseHP=30, speed=2f, armor=0, goldReward=5 },
            new EnemyDefinition { type=EnemyType.Orc, displayName="Orc", color=new Color(0.4f,0.5f,0.2f), baseHP=80, speed=1.5f, armor=2, goldReward=10 },
            new EnemyDefinition { type=EnemyType.Wolf, displayName="Wolf", color=new Color(0.5f,0.5f,0.5f), baseHP=25, speed=3.5f, armor=0, goldReward=8 },
            new EnemyDefinition { type=EnemyType.Skeleton, displayName="Skeleton", color=new Color(0.9f,0.9f,0.85f), baseHP=50, speed=2f, armor=0, goldReward=8, canResurrect=true },
            new EnemyDefinition { type=EnemyType.ShieldOrc, displayName="Shield Orc", color=new Color(0.35f,0.45f,0.25f), baseHP=100, speed=1.3f, armor=5, goldReward=15, shieldHits=3 },
            new EnemyDefinition { type=EnemyType.Bat, displayName="Bat Swarm", color=new Color(0.3f,0.2f,0.35f), baseHP=15, speed=2.5f, armor=0, goldReward=3, flying=true },
            new EnemyDefinition { type=EnemyType.DarkMage, displayName="Dark Mage", color=new Color(0.4f,0.15f,0.5f), baseHP=60, speed=1.8f, armor=1, goldReward=12, healAuraRadius=2f, healPerSecond=5f },
            new EnemyDefinition { type=EnemyType.Golem, displayName="Golem", color=new Color(0.5f,0.45f,0.4f), baseHP=250, speed=0.8f, armor=8, goldReward=25, immuneToSlow=true },
            new EnemyDefinition { type=EnemyType.Ghost, displayName="Ghost", color=new Color(0.7f,0.8f,0.9f,0.6f), baseHP=40, speed=2.2f, armor=0, goldReward=10, dodgeChance=0.5f },
            new EnemyDefinition { type=EnemyType.Dragon, displayName="Dragon", color=new Color(0.8f,0.15f,0.1f), baseHP=800, speed=1f, armor=10, goldReward=100, immuneToPoison=true, attackDamage=20, attackInterval=3f },
        };

        public static readonly WaveDefinition[] Map1Waves = new[]
        {
            new WaveDefinition { waveNumber=1, spawnInterval=1.0f, bonusGold=10, hint="Place an Arrow Tower!",
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Goblin, count=8} } },
            new WaveDefinition { waveNumber=2, spawnInterval=0.9f, bonusGold=10, hint="Upgrade your tower!",
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Goblin, count=12} } },
            new WaveDefinition { waveNumber=3, spawnInterval=0.8f, bonusGold=15,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Goblin, count=8}, new WaveEntry{enemyType=EnemyType.Orc, count=3} } },
            new WaveDefinition { waveNumber=4, spawnInterval=0.5f, bonusGold=12,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Wolf, count=10} } },
            new WaveDefinition { waveNumber=5, spawnInterval=0.7f, bonusGold=20,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Goblin, count=6}, new WaveEntry{enemyType=EnemyType.Wolf, count=4}, new WaveEntry{enemyType=EnemyType.Orc, count=2} } },
            new WaveDefinition { waveNumber=6, spawnInterval=0.9f, bonusGold=15,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Skeleton, count=8} } },
            new WaveDefinition { waveNumber=7, spawnInterval=0.8f, bonusGold=18,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Orc, count=5}, new WaveEntry{enemyType=EnemyType.Skeleton, count=6} } },
            new WaveDefinition { waveNumber=8, spawnInterval=0.7f, bonusGold=20,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.ShieldOrc, count=4}, new WaveEntry{enemyType=EnemyType.Goblin, count=10} } },
            new WaveDefinition { waveNumber=9, spawnInterval=0.4f, bonusGold=15,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Wolf, count=15}, new WaveEntry{enemyType=EnemyType.Goblin, count=5} } },
            new WaveDefinition { waveNumber=10, spawnInterval=0.6f, bonusGold=25,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Bat, count=12} } },
            new WaveDefinition { waveNumber=11, spawnInterval=0.8f, bonusGold=22,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Orc, count=5}, new WaveEntry{enemyType=EnemyType.ShieldOrc, count=4}, new WaveEntry{enemyType=EnemyType.Skeleton, count=4} } },
            new WaveDefinition { waveNumber=12, spawnInterval=0.7f, bonusGold=25,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.DarkMage, count=3}, new WaveEntry{enemyType=EnemyType.Orc, count=8} } },
            new WaveDefinition { waveNumber=13, spawnInterval=0.5f, bonusGold=20,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Wolf, count=8}, new WaveEntry{enemyType=EnemyType.Bat, count=6} } },
            new WaveDefinition { waveNumber=14, spawnInterval=0.6f, bonusGold=30,
                entries=new[]{ new WaveEntry{enemyType=EnemyType.ShieldOrc, count=5}, new WaveEntry{enemyType=EnemyType.DarkMage, count=3}, new WaveEntry{enemyType=EnemyType.Skeleton, count=5} } },
            new WaveDefinition { waveNumber=15, spawnInterval=0.3f, bonusGold=50, hint="FINAL WAVE!",
                entries=new[]{ new WaveEntry{enemyType=EnemyType.Dragon, count=1}, new WaveEntry{enemyType=EnemyType.Goblin, count=20} } },
        };

        public static TowerDefinition GetTower(TowerType t)
        {
            foreach (var td in Towers) if (td.type == t) return td;
            return Towers[0];
        }

        public static EnemyDefinition GetEnemy(EnemyType t)
        {
            foreach (var ed in Enemies) if (ed.type == t) return ed;
            return Enemies[0];
        }
    }
}
