using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    [SerializeField] public string SceneName = "Starting Scene";
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        //Destroy(Camera.main.gameObject);
        NetworkManager.Singleton.SceneManager.LoadScene(SceneName,LoadSceneMode.Single);
    }
    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();

    }
}
