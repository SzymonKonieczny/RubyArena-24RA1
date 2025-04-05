using System.Collections;
using System;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class CharacterCardScript : MonoBehaviour
{
    public CharacterCardSO currentCharacter;
    public TMPro.TMP_Text name;
    public Image image;
    public Action<CharacterCardScript> onSelected;
    public void SetDisplay(CharacterCardSO cardSO)
    {
        name.text = cardSO.name;
        image.sprite = cardSO.image;
        currentCharacter = cardSO;
    }
    public void onClick()
    {
        onSelected.Invoke(this);
    }
}
