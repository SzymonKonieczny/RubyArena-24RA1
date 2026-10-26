using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Minimal animator wrapper for mobs. Compatible with PlayerAnimationScript's interface
/// so skills/attacks can use either interchangeably.
/// </summary>
public class MobAnimationScript : NetworkBehaviour
{
    [SerializeField] Animator animator;

    public void SetAnimator(Animator ani) => animator = ani;

    public void PlayState(string stateName, float? time = null)
    {
        if (animator == null) return;
        if (time == null)
            animator.Play(stateName);
        else
            animator.CrossFade(stateName, time.Value);
    }

    public void Trigger(string triggerName)
    {
        if (animator == null) return;
        animator.SetTrigger(triggerName);
    }

    public void SetFloat(string name, float value)
    {
        if (animator == null) return;
        animator.SetFloat(name, value);
    }
}
