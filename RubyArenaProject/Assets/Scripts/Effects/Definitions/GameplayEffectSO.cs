using UnityEngine;

/// <summary>
/// Inspector-authored definition of a gameplay effect: its config numbers, its
/// cosmetic prefab, and a factory that builds the runtime <see cref="GameplayEffect"/>.
///
/// The client needs this to turn the wire id (<see cref="registryId"/>) back into the
/// correct subclass — see EffectRegistry. Subclass this per effect type and implement
/// CreateEffect().
/// </summary>
public abstract class GameplayEffectSO : ScriptableObject
{
    [Tooltip("Stable dedup/stacking key, e.g. 'dot_burn'. Two effects with the same " +
             "value from the same source collapse into one.")]
    public string effectId;

    [Tooltip("Fill in automatically via the EffectRegistry window. Do not hand-edit once " +
             "builds have shipped.")]
    public ushort registryId;

    [Tooltip("How many times the effect can stack when reapplied from the same source. " +
             "1 = pure refresh.")]
    public int maxStacks = 1;

    [Tooltip("Seconds the effect lasts from application. The holder turns this into an " +
             "absolute serverTimeEffectEnd; effects store that, never remaining time.")]
    public float duration = 5f;

    /// <summary>Convenience accessor matching GameplayEffect.Duration.</summary>
    public float Duration => duration;

    [Tooltip("Optional VFX spawned on the target while the effect is active (clients only).")]
    public GameObject cosmeticPrefab;

    [Tooltip("Where the cosmetic prefab is parented. Defaults to the target's transform.")]
    public string cosmeticBoneName = "";

    /// <summary>Build the runtime effect with this definition's config applied.
    /// Called on BOTH server (to apply) and client (to mirror a snapshot).</summary>
    public abstract GameplayEffect CreateEffect();
}
