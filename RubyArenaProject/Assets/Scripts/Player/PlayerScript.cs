using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;

public class PlayerScript : NetworkBehaviour
{
    [SerializeField] Transform[] CameraTransforms;
    [SerializeField]  GameObject corsshair;
    public NetworkVariable<bool> isStunnedNetworkVar = new NetworkVariable<bool>();

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
        {
            foreach (Transform t in CameraTransforms)
            {
                Destroy(t.GetComponent<CinemachineFreeLook>());
            }

            corsshair.SetActive(false);
        }

    }
    void Update()
    {
    }
   
}
