using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class UnitResource : NetworkBehaviour
{

    [SerializeField] public NetworkVariable<float> Hp = new NetworkVariable<float>(100);
    [SerializeField] public NetworkVariable<float> Mana = new NetworkVariable<float>(100);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="skillData">Takes SkillDataSO, to track the damage dealer</param>
    public abstract void damage(SkillDataSO skillData);
    public abstract void takeMana(float amount);
    public abstract float getHP();
    public abstract float getMana();

}
