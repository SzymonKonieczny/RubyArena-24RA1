using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterList : MonoBehaviour
{
    [SerializeField] public List<CharacterCardSO> Characters;

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
