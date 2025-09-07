using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SerializeField]
[CreateAssetMenu(fileName = "NewChracter", menuName = "ScriptableObjects/CharacterModelSO", order = 1)]
public class CharacterModelSO : ScriptableObject
{
    [SerializeField] public GameObject Model;
    [SerializeField] public GameObject AutoAttack;
    [SerializeField] public GameObject SkillPrefab1;
    [SerializeField] public GameObject SkillPrefab2;

    [SerializeField] public AutoAttackParams AutoAttackParams = new();

    //[SerializeField] public SkillDataSO Skill1;

}
