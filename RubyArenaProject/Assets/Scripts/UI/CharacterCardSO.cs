using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "NewChracter", menuName = "ScriptableObjects/CharacterCard", order = 1)]

public class CharacterCardSO : ScriptableObject
{
    public Sprite image;
    public string name;
    public int characterID;
    public CharacterModelSO characterModel;
}
