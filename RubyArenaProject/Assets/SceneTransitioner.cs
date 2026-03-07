using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitioner : MonoBehaviour
{
    [SerializeField] string sceneName;
    [SerializeField] float delay;
    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnConnectionLost;
        StartCoroutine(ChangeScene(delay, sceneName));
    }
    void OnConnectionLost(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            SceneManager.LoadScene("MultiplayerStartScene");
        }
    }

    IEnumerator ChangeScene(float delay, string name)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(name, LoadSceneMode.Single);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
