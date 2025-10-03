using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyCanvasManager : NetworkBehaviour
{
    [SerializeField] GameObject playerInLobbyCardPrefab;
    [SerializeField] NetworkObject PlayersListNO;
    [SerializeField] CharacterSelectUIScript characterSelect;
    [SerializeField] Button startButton;
    [SerializeField] TMPro.TMP_Text startButtonText;

    public Dictionary<ulong, PlayerInLobbyIcon> playersInLobby = new();
    void PostServerPlayerStateManagerInitialize()
    {
        ServerPlayerStateManager.Instance.onPlayerStatesChanged += UpdateStartButton;
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
         
        startButton.interactable = IsServer;
        if (!IsServer) return;


        NetworkManager.Singleton.OnClientConnectedCallback += SpawnCardForJoiningPlayer;
        NetworkManager.Singleton.OnClientDisconnectCallback += DespawnCardForLeavingPlayer;

        foreach (var player in ServerPlayerStateManager.Instance.playerStates)
        {
            SpawnCardForJoiningPlayer(player.Key);
        }
    }
    void UpdateStartButton(LocalPlayerStateManager updated)
    {
        int ready = 0;
        int allPlayers = ServerPlayerStateManager.Instance.playerStates.Count;
        foreach (var player in ServerPlayerStateManager.Instance.playerStates)
        {
            if (player.Value.chosenCharacter.Value >= 0)
                ready += 1;
        }
        startButtonText.text = $"Start {ready}/{allPlayers}";
        startButton.interactable = (ready == allPlayers) && IsServer;

    }
    void DespawnCardForLeavingPlayer(ulong clientId)
    {
        playersInLobby[clientId].NetworkObject.Despawn();
    }
    void SpawnCardForJoiningPlayer(ulong clientId)
    {
        var obj = Instantiate(playerInLobbyCardPrefab);
        var playerInLobbyIcon = obj.GetComponent<PlayerInLobbyIcon>();

        playerInLobbyIcon.NetworkObject.SpawnWithOwnership(clientId);
        playerInLobbyIcon.NetworkObject.TrySetParent(PlayersListNO);

        playersInLobby.Add(clientId,playerInLobbyIcon);
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.Singleton.OnClientConnectedCallback -= SpawnCardForJoiningPlayer;
        NetworkManager.Singleton.OnClientDisconnectCallback -= DespawnCardForLeavingPlayer;

    }
    public void SwitchCharacterSelectUI()
    {
        characterSelect.gameObject.SetActive(!characterSelect.gameObject.activeInHierarchy);
    }
    public void TESTChangeChamp()
    {
        LocalPlayerStateManager.LocalInstance.chosenCharacter.Value = Random.RandomRange(0, 3);
    }
    public void StartGame()
    {
        UpdateStartButton(null);
        if (!IsServer) return;
        NetworkManager.Singleton.SceneManager.LoadScene("AmityArena", LoadSceneMode.Single);

    }
}
