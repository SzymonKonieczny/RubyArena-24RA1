using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntitySpawningSkill : BaseSkill
{
    public override bool CanCast()
    {
        return Time.time - LastUsed > SkillDataSO.coolDown;
    }

    public override void ChangeSkilRequestState(SkillRequestState newState)
    {
        SkillRequestState = newState;
        OnRequestStateChange(newState);
    }

    public override void OnRequestStateChange(SkillRequestState state)
    {
        if(state == SkillRequestState.Accepted)
        {
            animator.PlayState(SkillDataSO.animationCastName);
        }
        else
        {
            animator.PlayState("Idle");
        }
        SkillRequestState = SkillRequestState.NotRequested;
    }

    public override void SkillRequested()
    {
        LastUsed = Time.time;
        animator.PlayState(SkillDataSO.animationWindUpName);

    }

    
}
