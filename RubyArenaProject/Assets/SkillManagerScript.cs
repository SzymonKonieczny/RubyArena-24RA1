using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public class SkillManagerScript : NetworkBehaviour
{
    public static float ServerTime = 0; /// <summary>
    /// Server time elapsed from the start of the Host simulation
    /// </summary>

   public static List<Action<int>> SkillList = new List<Action<int>>();

    void Start()
    {
        SkillList.Add(testSkill);
    }

    void Update()
    {
        //ToDo: Control every players position if its suspicious, potentially block it.

    }

    void testSkill(int input)
    {
        Debug.Log($"TEST SKILL USED WITH {input}");  
    }
}
