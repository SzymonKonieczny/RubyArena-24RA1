using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Mob-specific CombatEntity implementation. Stun freezes the mob's AI instead of player input.
/// </summary>
public class MobCombatEntity : CombatEntity
{
    [SerializeField] MobAnimationScript mobAnimationScript;
    [SerializeField] MobMovement mobMovement;
    [SerializeField] MobAI mobAI;

    public override bool CanCombat { get; protected set; } = true;
    public override PlayerAnimationScript AnimationScript => null; // Mobs use MobAnimationScript, not PlayerAnimationScript
    public override Movement PlayerMove => null; // Mobs use MobMovement, not Movement

    /// <summary>
    /// The mob-specific animation controller. Skills that need animation should check this.
    /// </summary>
    public MobAnimationScript MobAnimation => mobAnimationScript;

    /// <summary>
    /// The mob-specific movement controller.
    /// </summary>
    public MobMovement MobMovement => mobMovement;

    public MobAI MobAI => mobAI;

    public override void SetStunTimer(float time)
    {
        if (mobAI == null)
        {
            Debug.LogWarning($"No MobAI found for {this.name}");
            return;
        }

        mobAI.SetStun(time);
    }

    /// <summary>
    /// Applies knockback to the mob. Server-only since mobs are server-owned.
    /// </summary>
    public void ApplyKnockback(Vector3 velocity)
    {
        if (!IsServer) return;
        mobMovement?.ApplyKnockback(velocity);
    }

    public void Initialize()
    {
        mobAnimationScript = GetComponent<MobAnimationScript>();
        mobMovement = GetComponent<MobMovement>();
        mobAI = GetComponent<MobAI>();

        // Set a default skillshot spawn point (mob's position)
        if (SkillshotSpawnPoint == null)
            SetSkillshotSpawnPoint(transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("NonFightArea"))
            CanCombat = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("NonFightArea"))
            CanCombat = true;
    }
}
