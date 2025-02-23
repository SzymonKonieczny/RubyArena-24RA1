using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterList : MonoBehaviour
{
    public List<CharacterModelSO> Chracters = new();

    public static CharacterList Instance;

  

    private void Start()
    {
        if (Instance != this)
            Instance = this;
    }
    private CharacterList()
    {
        Instance = this;
    }
}
