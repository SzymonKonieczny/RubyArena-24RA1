using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System;

public class PlayerInLobbyIcon : NetworkBehaviour
{
    public NetworkVariable<int> chosenCharacterId = new NetworkVariable<int>(value : -1,writePerm: NetworkVariableWritePermission.Owner); //Could be us, could be other players
    public Button Button;
    public Image Icon;
    public Action OnButtonClick;
    public override void OnNetworkSpawn()
    { 
        base.OnNetworkSpawn();

        if(chosenCharacterId.Value >0) //catch up if they were in the lobby before us
        {
            Icon.sprite = CharacterList.Instance.Characters[chosenCharacterId.Value].image;
        }
        chosenCharacterId.OnValueChanged += (int prevV, int newV) =>
        {
            Icon.sprite = CharacterList.Instance.Characters[newV].image;
        };
        LocalPlayerStateManager.LocalInstance.chosenCharacter.OnValueChanged += UpdateUIIcon;
        Button.gameObject.SetActive(IsOwner);

        if(IsOwner)
        {
            LobbyCanvasManager lobbyCanvas = GameObject.FindAnyObjectByType<LobbyCanvasManager>();
            Button.onClick.AddListener(lobbyCanvas.SwitchCharacterSelectUI);
        }
    }
    void UpdateUIIcon(int prevV, int newV)
    {
        if (!IsOwner) return;

        chosenCharacterId.Value = newV;
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        LocalPlayerStateManager.LocalInstance.chosenCharacter.OnValueChanged -= UpdateUIIcon;

    }

}
