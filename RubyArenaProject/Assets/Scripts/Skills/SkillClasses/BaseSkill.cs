using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// Base class for skills sitting in the players "inventory" (skill slots in CombatManager)
/// Object-ifing the management of individual skills such as their usage, what animations they should trigger etc.
/// </summary>
[Serializable]
public abstract class BaseSkill 
{
    [SerializeField] public SkillDataSO SkillDataSO;

    [SerializeField] public SkillRequestState SkillRequestState { protected set;  get; }  //for local purposes, animations and stuff

    [SerializeField] public float LastUsed;

    [SerializeField] public PlayerAnimationScript animator;

    public abstract void ChangeSkilRequestState(SkillRequestState newState);

    public abstract void OnRequestStateChange(SkillRequestState state);

    public abstract bool CanCast();
  

}
public enum SkillRequestState
{
    NotRequested,
    Requested,
    Accepted,
    Rejected
}
