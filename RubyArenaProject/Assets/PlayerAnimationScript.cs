using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SkillAnimationOptions
{
    public float Speed;
    public string StateName;
}
public class PlayerAnimationScript : NetworkBehaviour
{
    Rigidbody Rb;
    [SerializeField] Animator animator;
    [SerializeField] float velo;
    Movement PlayerMove;
    void Start()
    {
        Rb = GetComponent<Rigidbody>();
        PlayerMove = GetComponent<Movement>();
    }
    public void setAnimatorField(Animator ani)
    {
        animator = ani;
    }
    // Update is called once per frame
    void Update()
    {
        if (animator == null) return;

        velo = PlayerMove.RbVelocityNetworkVar.Value.magnitude;

       animator.SetFloat("Velocity", velo);
    }
    public void PlayState(SkillAnimationOptions options)
    {
        var temp = animator.speed;
        animator.speed = options.Speed;
        animator.Play(options.StateName);
        animator.speed = temp;
    }
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
}
