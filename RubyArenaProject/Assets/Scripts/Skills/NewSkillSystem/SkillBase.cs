using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


public abstract class SkillBase : NetworkBehaviour
{
    public PlayerCombatManager combatManagerRef;

    public PlayerAnimationScript animationScript;

    public InputCollectorScript InputCollector;

    [SerializeField] public SkillDataSO SkillDataSO;

    public abstract bool Use();
}
