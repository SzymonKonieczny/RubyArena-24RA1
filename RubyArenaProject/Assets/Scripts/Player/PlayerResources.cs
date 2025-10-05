using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PlayerResources : UnitResource
{
    [SerializeField] Slider HP_Slider;
    [SerializeField] Slider Mana_Slider;
    
    [SerializeField] float MaxHP = 100;
    [SerializeField] float MaxMana = 100;
    [SerializeField] ParticleSystem bleedingEffect;

    public System.Action<float> onTakeDamage ;
    private PlayerScript Player; //optimization to call getComponent less
    public void SetMaxHP(float amount)
    {
        MaxHP = amount;
    }
    public void SetMaxMana(float amount)
    {
        MaxMana = amount;
    }
    public float getMaxHP()
    {
        return MaxHP;
    }


    public override void damage(SkillDataSO skillData)
    {
        if (!IsServer) return;

        Hp.Value -= skillData?.damage ?? 0;
        onTakeDamage?.Invoke(skillData.damage);
        if(Player == null)
        {
         if(!TryGetComponent<PlayerScript>(out Player))
            {
                Debug.LogError("Unable to get this player");
            }
        }
        PlayerScript? DamageDealer = null;
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(skillData.ownerId, out NetworkObject networkObject))
        {
           DamageDealer = networkObject.GetComponent<PlayerScript>();
        }
        GameModeManager.Instance().OnPlayerTakeDmg.Invoke(Player, skillData.damage, DamageDealer);
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
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }
    public void Initialize()
    {
        if(IsServer)
        {
            Hp.Value = MaxHP;
            Mana.Value = MaxMana;
        }

        Hp.OnValueChanged += (float preV, float newV) =>
        {
            if (preV > newV)
            {
                bleedingEffect.emission.SetBurst(0, new ParticleSystem.Burst(0, (preV - newV) * 3));
                bleedingEffect.Play();
            }
        };

        if (!IsOwner) return;

        var GO = GameObject.FindGameObjectWithTag("HealthBar");
        HP_Slider= GO?.GetComponent<Slider>();
      

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
