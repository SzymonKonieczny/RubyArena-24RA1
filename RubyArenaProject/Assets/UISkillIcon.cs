using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISkillIcon : MonoBehaviour
{
    [SerializeField]  public SkillCastType skillType;
    [SerializeField] Slider slider;
    [SerializeField]  SkillBase skill;
    [SerializeField]  DateTime nextAvaliable;
    [SerializeField]  float cooldown;
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
        skill.nextAvaliableTicks.OnValueChanged += (long oldV, long newV) =>
        {
            nextAvaliable = new DateTime(newV);
        };
        cooldown = skill.cooldown;
    }

    void Update()
    {
        float secondsRemaming = Mathf.Clamp((float)(nextAvaliable - DateTime.UtcNow).TotalSeconds,0,99);
        slider.value = 1 - (secondsRemaming/cooldown);
    }
}
