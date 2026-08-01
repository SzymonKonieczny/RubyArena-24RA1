using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;
using Cinemachine;
using VContainer;
using Assets.Scripts.Events;

public class PlayerResources : UnitResource
{
    [SerializeField] Slider HP_Slider;
    [SerializeField] Slider Mana_Slider;
    [SerializeField] float MaxHP = 100;

    [SerializeField] float MaxMana = 100;
    [SerializeField] ParticleSystem bleedingEffect; //inside the player prefab. Assinged via editor
    GameObject HealthBarObject;
    CinemachineImpulseSource impulseSource;
    
    [Inject]
    public IGameMode? gameMode;

    [Inject]
    EventBus<DamageTakenEvent> damageEventBus;

    [Inject]
    EventBus<PlayerDeathEvent> playerDeathEvent;

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

    public override void damage(SkillInstanceData skillData)
    {
        if (!IsServer) return;
        if(gameMode !=null && !gameMode.CanDamage(this.NetworkObjectId))
        {
            return;
        }
        float hpBefore = Hp.Value;
        Hp.Value -= skillData.damage;


        damageEventBus.Raise( new DamageTakenEvent {
            damagerNetworkObjectId = skillData.ownerNetworkObjectId,
            recieverNetworkObjectId = this.NetworkObjectId,
             healthBefore = hpBefore,
             healthAfter = Hp.Value,
             spellCarrierNetworkObjectId = skillData.spellCarrierNetworkObjectId,
             damageAmountPreMitigation = skillData.damage,
             damageAmountPostMitigation = skillData.damage,
        });
            

        if(Hp.Value <=0)
        {
            playerDeathEvent.Raise(new PlayerDeathEvent { 
                playerKillingNetworkId = skillData.ownerNetworkObjectId,
                playerKilledNetworkId =  this.NetworkObject.NetworkObjectId });
        }
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
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += onLoadComplete;
    }
    void onLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsOwner) return;

        HealthBarObject = GameObject.FindGameObjectWithTag("HealthBar");
        HP_Slider = HealthBarObject?.GetComponent<Slider>();
        impulseSource = gameObject.GetComponentInChildren<CinemachineImpulseSource>();
        if (gameMode == null)
        {
            Debug.LogError("Player resources running without a gamemode. This may cause null refernces or unintended behavior");
        }

        Mana.OnValueChanged += (float preV, float newV) =>
        {
            Mana_Slider.value = newV / MaxMana;
        };

        Hp.OnValueChanged += (float preV, float newV) =>
        {
            HP_Slider.value = newV / MaxHP;
            if (preV > newV)
            {
                impulseSource?.GenerateImpulse(0.5f - 0.5f/(preV-Mathf.Abs(newV)));
            }
        };

        Hp.OnValueChanged.Invoke(0, MaxHP);
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= onLoadComplete;
    }



}
