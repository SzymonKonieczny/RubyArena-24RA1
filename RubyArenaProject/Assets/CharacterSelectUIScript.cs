using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUIScript : MonoBehaviour
{
    [SerializeField] List<CharacterCardScript> characterCards;
    [SerializeField] Transform cardHolderTransform;
    [SerializeField] GridLayoutGroup cardHolderLayout;

    [SerializeField] CharacterCardScript chosenCharacter;
    [SerializeField] List<CharacterCardSO> allCharacters;
    [SerializeField] GameObject characterCardPrefab;
    [SerializeField] public PlayerScript playerScript; //initialized by the playerscript on spawn

    // Start is called before the first frame update
    void Start()
    {
        cardHolderLayout = cardHolderTransform.GetComponent<GridLayoutGroup>();
        int screenSize = Screen.width;
        cardHolderLayout.cellSize = new Vector2(screenSize / 10, screenSize / 10);
        foreach (var character in allCharacters)
        {
            GameObject cardObj = Instantiate(characterCardPrefab, cardHolderTransform);
            CharacterCardScript cardScript = cardObj.GetComponent<CharacterCardScript>();
            characterCards.Add(cardScript);
            cardScript.SetDisplay(character);
            cardScript.onSelected += onSelectedCharacter;
        }

    }
    void onSelectedCharacter(CharacterCardScript selected)
    {
        playerScript.AskToSelectCharacterServerRpc(selected.currentCharacter.characterID);

        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
    }
}
