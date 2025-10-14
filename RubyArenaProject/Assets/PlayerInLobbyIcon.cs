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
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeIcon();
    }
    void InitializeIcon()
    {
        if (chosenCharacterId.Value > 0) //catch up if they were in the lobby before us
        {
            Icon.sprite = CharacterList.Instance.Characters[chosenCharacterId.Value].image;
        }
        chosenCharacterId.OnValueChanged += (int prevV, int newV) =>
        {
            Icon.sprite = CharacterList.Instance.Characters[newV].image;
        };
        LocalPlayerStateManager.LocalInstance.chosenCharacter.OnValueChanged += UpdateUIIcon;
        Button.gameObject.SetActive(IsOwner);

        if (IsOwner)
        {
            Button.onClick.AddListener(TemporaryFunctionBecauseSceneLoadingCallbacksDoNotFireAndOnNetworkSpawnCanFireBeforeTheSceneIsDoneLoadingXd);
            var canvasGO = GameObject.FindGameObjectWithTag("LobbyCanvas");
            lobbyCanvas = canvasGO.GetComponent<LobbyCanvasManager>();
        }
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
