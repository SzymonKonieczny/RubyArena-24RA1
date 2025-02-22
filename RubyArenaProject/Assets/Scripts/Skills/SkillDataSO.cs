using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "Skill", menuName = "ScriptableObjects/SkillData", order = 1)]
public class SkillDataSO : ScriptableObject
{
    public SkillType Type;

    //int id of a function in a global skill Funcs registry? Have it take args of the player script ?
    // or an Enum so its easier to keep track of in editor, and then cast that enum to an int ?
    public int damage;

    public ulong ownerId;

    public float castTime;

    public float coolDown;

    /// <summary>
    /// a prefab with a derived class of SkillBehavior
    /// </summary>
    public GameObject SkillEntityToSpawn;

    public string animationWindUpName;
    public string animationCastName;



}


[System.Serializable]
public enum SkillType
{
    EntitySpawner, //spawn a small ball that dashes you into a direction?
    Seroid,
    Dash

}
