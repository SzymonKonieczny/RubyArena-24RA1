using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerResources : UnitResource
{
    [SerializeField] Slider HP_Slider;
    [SerializeField] Slider Mana_Slider;
    
    [SerializeField] float MaxHP = 100;
    [SerializeField] float MaxMana = 100;
    public void SetMaxHP(float amount)
    {
        MaxHP = amount;
    }
    public void SetMaxMana(float amount)
    {
        MaxMana = amount;
    }

    public override void damage(float amount)
    {
        if (!IsServer) return;

        Hp.Value -= amount;
    }

    public override float getHP()
    {
        return Hp.Value;
    }

    public override float getMana()
    {
        return Mana.Value;
    }

    public override void takeMana(float amount)
    {
        if (!IsServer) return;

        Mana.Value -= amount;

    }
    private void Start()
    {
        if(IsServer)
        {
            Hp.Value = MaxHP;
            Mana.Value = MaxMana;
        }

        if (!IsOwner) return;


        HP_Slider = GameObject.FindGameObjectWithTag("HealthBar")?.GetComponent<Slider>();
      

        Mana.OnValueChanged += (float preV, float newV) =>
        {
            Mana_Slider.value = newV / MaxMana;
        };

        Hp.OnValueChanged += (float preV, float newV) =>
        {
            HP_Slider.value = newV / MaxHP;
        };

        Hp.OnValueChanged.Invoke(0, MaxHP);

    }


}
