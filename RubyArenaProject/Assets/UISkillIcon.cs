using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UISkillIcon : MonoBehaviour
{
    [SerializeField]  public SkillCastType skillType;
    [SerializeField] Slider slider;
    [SerializeField]  SkillBase skill;
    [SerializeField]  double nextAvaliableTime;
    [SerializeField]  float cooldown;
    [SerializeField] Image icon;
    // Start is called before the first frame update
    void Awake() //should be called before any network stuff
    {
       if(LocalPlayerStateManager.LocalInstance.localPlayerRef) // Inconsistent function call order forced my hand, forgive me God
       {
           Init();
       }
       else
       {
           LocalPlayerStateManager.LocalInstance.localPlayerInitialized += Init;
       }
    }
    void onLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Init();
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= onLoadComplete;
    }
    private void Init()
    {
        var skills = LocalPlayerStateManager.LocalInstance.localPlayerRef.GetComponentsInChildren<SkillBase>();
        foreach (var s in skills)
        {
            if (s.castType == this.skillType)
            {
                skill = s;
            }
        }
        skill.nextAvaliable.OnValueChanged += (double oldV, double newV) =>
        {
            nextAvaliableTime = newV;

        };
        if (skill)
        {
            cooldown = skill.cooldown;
            icon.sprite = skill.SkillDataSO.Icon;
        }
        LocalPlayerStateManager.LocalInstance.localPlayerInitialized -= Init;
    }

    void Update()
    {
        double serverTime = NetworkManager.Singleton.NetworkTimeSystem.ServerTime;
        float secondsRemaming = Mathf.Clamp((float)(nextAvaliableTime - serverTime),0,99);
        slider.value = Mathf.Clamp((secondsRemaming/cooldown),0,1);
    }
}
