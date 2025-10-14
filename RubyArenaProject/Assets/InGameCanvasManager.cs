using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InGameCanvasManager : MonoBehaviour
{
    
    [SerializeField] GameObject canvasRoot;

    private void Start()
    {
        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        canvasRoot.SetActive(false);
    }
    private void OnDestroy()
    {
        UnityEngine.Cursor.visible = transform;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = (CursorLockMode)((int)Cursor.lockState ^ 1);
            canvasRoot.SetActive(!canvasRoot.activeInHierarchy);
        }
    }
}
