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
    [SerializeField] private TMPro.TMP_Text DisplayJoinCode;
    [SerializeField] public string SceneName = "Starting Scene";
    async Task InitServices()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void StartHost()
    {

        if (useRelay.isOn)
        {
            await InitServices();
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            DisplayJoinCode.text = code;
            var relayServerData = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

        }

        NetworkManager.Singleton.OnClientConnectedCallback += (ulong id) =>
        {
            //NetworkManager.Singleton.SceneManager.OnSceneEvent
        };
        StartMap();
    }

    public async void StartMap()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
    }
    public async void StartClient()
    {
        if (useRelay.isOn)
        {
            await InitServices();

            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(InputJoinCode.text);
            var relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        }
        NetworkManager.Singleton.StartClient();

    }
    private async void Start()
    {

    }

    private void Singleton_OnClientConnectedCallback(ulong obj)
    {
        throw new System.NotImplementedException();
    }
}
