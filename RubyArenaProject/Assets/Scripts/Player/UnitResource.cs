using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public abstract class UnitResource : NetworkBehaviour
{

   [SerializeField] public NetworkVariable<float> Hp = new NetworkVariable<float>();
    [SerializeField] public NetworkVariable<float> Mana = new NetworkVariable<float>();



    public abstract void damage(float amount);
    public abstract void takeMana(float amount);
    public abstract float getHP();
    public abstract float getMana();

}
