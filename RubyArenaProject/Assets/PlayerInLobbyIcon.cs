using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System;
using UnityEngine.SceneManagement;

public class PlayerInLobbyIcon : NetworkBehaviour
{
    public NetworkVariable<int> chosenCharacterId = new NetworkVariable<int>(value : -1,writePerm: NetworkVariableWritePermission.Owner); //Could be us, could be other players
    public Button Button;
    public Image Icon;
    public Action OnButtonClick;
    LobbyCanvasManager lobbyCanvas;
    public Sprite defaultIcon;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeIcon();
    }
    void InitializeIcon()
    {
        if (IsOwner)
        {
            int? lastChosenChamp = LocalPlayerStateManager.LocalInstance?.chosenCharacter?.Value;
            chosenCharacterId.Value = lastChosenChamp ?? -1;
            Button.onClick.AddListener(TemporaryFunctionBecauseSceneLoadingCallbacksDoNotFireAndOnNetworkSpawnCanFireBeforeTheSceneIsDoneLoadingXd);
            var canvasGO = GameObject.FindGameObjectWithTag("LobbyCanvas");
            lobbyCanvas = canvasGO.GetComponent<LobbyCanvasManager>();

        }

        if (chosenCharacterId.Value >= 0) //catch up if they were in the lobby before us
        {
            Icon.sprite = CharacterList.Instance.Characters[chosenCharacterId.Value].image;
        }
        chosenCharacterId.OnValueChanged += (int prevV, int newV) =>
        {
            if (newV < 0)
            {
                Icon.sprite = defaultIcon;
                return;
            }
            Icon.sprite = CharacterList.Instance.Characters[newV].image;
        };
        LocalPlayerStateManager.LocalInstance.chosenCharacter.OnValueChanged += UpdateUIIcon;
        Button.gameObject.SetActive(IsOwner);

   
    }
    void TemporaryFunctionBecauseSceneLoadingCallbacksDoNotFireAndOnNetworkSpawnCanFireBeforeTheSceneIsDoneLoadingXd()
    {
        if(lobbyCanvas == null)
        {
            var canvasGO = GameObject.FindGameObjectWithTag("LobbyCanvas");
            lobbyCanvas = canvasGO.GetComponent<LobbyCanvasManager>();
        }
        lobbyCanvas.SwitchCharacterSelectUI();
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
