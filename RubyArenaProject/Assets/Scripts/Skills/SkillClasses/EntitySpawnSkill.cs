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
        switch (state){
            case SkillRequestState.Requested:
                LastUsed = Time.time;
                animator.PlayState(SkillDataSO.animationWindUpName, SkillDataSO.windUpTime);
                animator.PlayState(new SkillAnimationOptions {Speed = 1f/ SkillDataSO.windUpTime, StateName = SkillDataSO.animationWindUpName });

                break;
            case SkillRequestState.Accepted:
                //       animator.PlayState(SkillDataSO.animationCastName, SkillDataSO.castTime);
                animator.PlayState(new SkillAnimationOptions { Speed = 1f / SkillDataSO.castTime, StateName = SkillDataSO.animationCastName });
                animator.Trigger("SpellCastAccepted");
                SkillRequestState = SkillRequestState.NotRequested; //soft change to default

                break;
            case SkillRequestState.Rejected:
                animator.PlayState("Idle");
                SkillRequestState = SkillRequestState.NotRequested;//soft change to default

                break;

            default:
                animator.PlayState("Idle");
                Debug.Log($"State of: {nameof(state)} does not have defined behavior.");
                break;
        }
       
    }

  
    
}
