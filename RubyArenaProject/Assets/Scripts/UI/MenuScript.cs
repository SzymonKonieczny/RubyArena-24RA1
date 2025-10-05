using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.Networking.Transport.Relay;
using Unity.Netcode.Transports.UTP;
using UnityEngine.UI;
using System.Threading.Tasks;

public class MenuScript : MonoBehaviour
{

    [SerializeField] private Toggle useRelay;

    [SerializeField] private TMPro.TMP_InputField InputJoinCode;
    [SerializeField] public string SceneName = "Starting Scene";
    async Task InitServices()
    {
        await UnityServices.InitializeAsync();

        if(!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    public async void StartHost()
    {
        LogNetworkConfig();

        if (useRelay.isOn)
        {
            await InitServices();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            ServerPlayerStateManager.Instance.lobbyJoinCode = code;
            var relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        }

        NetworkManager.Singleton.OnClientConnectedCallback += (ulong id) =>
        {
            //NetworkManager.Singleton.SceneManager.OnSceneEvent
        };
        StartMap();

    }
    public void StartMap()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
    }
    public async void StartClient()
    {
        LogNetworkConfig();

        if (useRelay.isOn)
        {
            await InitServices();

            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(InputJoinCode.text);
            var relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        }
        NetworkManager.Singleton.StartClient();

    }

    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        throw new System.NotImplementedException();
    }
    void LogNetworkConfig()
    {
        var config = NetworkManager.Singleton.NetworkConfig;
        Debug.Log($"TickRate: {config.TickRate}, Transport: {config.NetworkTransport.GetType().Name}, Prefabs: {config.Prefabs.NetworkPrefabsLists.Count}");
    }
}
