using UnityEngine;

public enum MobAttackType
{
    Melee,
    RangedProjectile,
    AOE
}

public enum MobDisposition
{
    Passive,    // Never attacks unless attacked
    Neutral,    // Attacks only if attacked
    Aggressive  // Attacks anything in aggro range
}

[CreateAssetMenu(fileName = "MobConfig", menuName = "ScriptableObjects/MobConfig", order = 2)]
public class MobConfigSO : ScriptableObject
{
    public string mobName;
    public GameObject modelPrefab;
    public Stats baseStats;

    [Header("Basic Attack")]
    public int attackDamage = 10;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
    public MobAttackType attackType;

    [Header("AI Behavior")]
    public MobDisposition disposition;
    public float aggroRange = 10f;
    public float wanderRadius = 5f;
    public float attackWindup = 0.3f;

    [Header("Death")]
    public float deathDespawnDelay = 2f;
    public GameObject deathVFX;
    public string deathAnimationTrigger = "Death";
}
