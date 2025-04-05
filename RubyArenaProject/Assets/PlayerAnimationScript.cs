using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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
    public void PlayState(string stateName)
    {
        if (animator == null) return;

        animator.Play(stateName);
    }
    public void Trigger(string triggerName)
    {
        if (animator == null) return;

        animator.SetTrigger(triggerName);
    }
}
