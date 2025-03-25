using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum GameModeType
{
    OneVOne
}


[CreateAssetMenu(fileName = "NewGameMode", menuName = "ScriptableObjects/GameModeSO")]
public class GameModeSO : ScriptableObject
{
    public GameModeSO Gamemode;
    public string MapName;
}
