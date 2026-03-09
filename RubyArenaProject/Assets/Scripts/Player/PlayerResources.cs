using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;
using Cinemachine;

public class PlayerResources : UnitResource
{
    [SerializeField] Slider HP_Slider;
    [SerializeField] Slider Mana_Slider;

    [SerializeField] float MaxHP = 100;
    [SerializeField] float MaxMana = 100;
    [SerializeField] ParticleSystem bleedingEffect; //inside the player prefab. Assinged via editor
    public Action<ulong,ulong,float, float> onDamageDealt; //DamageDealer,DamageReciever,HpBefore,HpAfter
    public Action<ulong, ulong> onPlayerDeath; //Killer, Killed
    GameObject HealthBarObject;
    CinemachineImpulseSource impulseSource;
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


    public override void damage(SkillInstanceData skillData)
    {
        if (!IsServer) return;
        float hpBefore = Hp.Value;
        Hp.Value -= skillData.damage;
        onDamageDealt?.Invoke(skillData.ownerNetworkObjectId, this.NetworkObject.NetworkObjectId, hpBefore, Hp.Value);
        if(Hp.Value <=0)
        {
            onPlayerDeath?.Invoke(skillData.ownerNetworkObjectId, this.NetworkObject.NetworkObjectId);
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
