using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;

public class PlayerScript : NetworkBehaviour
{
    [SerializeField] Transform[] CameraTransforms;
    public NetworkVariable<bool> isStunnedNetworkVar = new NetworkVariable<bool>();
    public Transform ModelAnchor;
    public NetworkVariable <int> CharacterID;
    [SerializeField] PlayerAnimationScript PlayerAnimationScript;
    public override void OnNetworkSpawn()
    {

        if (IsServer)
        {
            CharacterID.Value = 0; //NetworkManager.ConnectedClients.Count-1; 
            InstantiateModelClientRPC(CharacterID.Value);
        }
        if (!IsLocalPlayer)
        {
            
            foreach (Transform t in CameraTransforms)
            {
                Destroy(t.GetComponent<CinemachineFreeLook>());
            }

        }

    }
   [ClientRpc]
    void InstantiateModelClientRPC(int id)
    {
        GameObject ModelGO = Instantiate(CharacterList.Instance.Chracters[CharacterID.Value].Model,ModelAnchor);
        ModelGO.transform.SetLocalPositionAndRotation(new Vector3(0, -1, 0), Quaternion.Euler(0, 0, 0));
        PlayerAnimationScript = GetComponent<PlayerAnimationScript>();

        Animator animator = ModelGO.transform.GetComponentInChildren<Animator>();
        PlayerAnimationScript.setAnimatorField(animator);
    }

    void Update()
    {
    }
   
}
