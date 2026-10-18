using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Base class for all networked combat entities (players, mobs, etc.).
/// Provides the common interface that skills and combat systems interact with.
/// </summary>
public abstract class CombatEntity : NetworkBehaviour
{
    [SerializeField] private Transform skillshotSpawnPoint;

    public Transform SkillshotSpawnPoint => skillshotSpawnPoint;
    public abstract bool CanCombat { get; protected set; }

    /// <summary>
    /// Animation controller for this entity. May be null on non-animated entities.
    /// </summary>
    public virtual PlayerAnimationScript AnimationScript => null;

    /// <summary>
    /// Movement controller for this entity. May be null on immobile entities.
    /// </summary>
    public virtual Movement PlayerMove => null;

    /// <summary>
    /// Applies a stun/timer effect to this entity. Implementation is entity-specific.
    /// For players this locks input; for mobs it may freeze AI behavior.
    /// Networking (ClientRpc etc.) is the responsibility of the derived class if needed.
    /// </summary>
    public abstract void SetStunTimer(float time);

    /// <summary>
    /// Allows derived classes to set the spawn point (e.g. fallback in Initialize).
    /// </summary>
    protected void SetSkillshotSpawnPoint(Transform t) => skillshotSpawnPoint = t;
}
