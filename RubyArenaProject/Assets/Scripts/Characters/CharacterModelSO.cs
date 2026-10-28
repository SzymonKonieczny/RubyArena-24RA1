using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SerializeField]
[CreateAssetMenu(fileName = "NewChracter", menuName = "ScriptableObjects/CharacterModelSO", order = 1)]
public class CharacterModelSO : ScriptableObject
{

    [SerializeField] public Sprite Icon;
    [SerializeField] public GameObject Model;
    [SerializeField] public GameObject AutoAttack;
    [SerializeField] public GameObject SkillPrefab1;
    [SerializeField] public GameObject SkillPrefab2;

    /// <summary>
    /// The base stats every modifier builds on top of. This is the FIRST write to the
    /// entity's StatModifierHolder: effects fold their contributions onto a copy of this,
    /// never onto each other. Populate every field — a zero AttackRange makes the auto
    /// attack's hitbox zero-sized, and it logs an error when that happens.
    /// </summary>
    [SerializeField] public Stats BaseStats = new();

}
