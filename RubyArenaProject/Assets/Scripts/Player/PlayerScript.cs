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
    public Transform ActiveModel;

    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            CharacterID.Value = NetworkManager.ConnectedClients.Count;
        }

    }

    public override void OnNetworkSpawn()
    {

    //    CharacterID.OnValueChanged += (int pre, int post) =>
    //    {
    //        InstantiateModel(post);
    //    };

        InstantiateModel(0);



        if (!IsLocalPlayer)
        {
            
            foreach (Transform t in CameraTransforms)
            {
                Destroy(t.GetComponent<CinemachineFreeLook>());
            }

        }

    }
    void InstantiateModel(int id)
    {
        if (ActiveModel != null)
        {
            Destroy(ActiveModel);
        }
        GameObject ModelGO = Instantiate(CharacterList.Instance.Chracters[CharacterID.Value % 2].Model,ModelAnchor);
        ModelGO.transform.SetLocalPositionAndRotation(new Vector3(0, -1, 0), Quaternion.Euler(0, 0, 0));
        ActiveModel = ModelGO.transform;
        PlayerAnimationScript = GetComponent<PlayerAnimationScript>();

        Animator animator = ModelGO.transform.GetComponentInChildren<Animator>();
        PlayerAnimationScript.setAnimatorField(animator);
    }

    void Update()
    {
    }
   
}
