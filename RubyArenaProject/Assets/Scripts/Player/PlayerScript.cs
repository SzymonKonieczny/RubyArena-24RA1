using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using UnityEngine.Rendering.HighDefinition.Attributes;

public class PlayerScript : NetworkBehaviour
{
    [SerializeField] Transform[] CameraTransforms;
    public NetworkVariable<bool> isStunnedNetworkVar = new NetworkVariable<bool>();
    public Transform ModelAnchor;
    public NetworkVariable <int> CharacterID;
    [SerializeField] PlayerAnimationScript PlayerAnimationScript;
    public Transform ActiveModel;
    public PlayerResources playerResources;
    public Movement playerMove;

    protected override void OnNetworkPostSpawn()
    {
        if (IsServer)
        {
            CharacterID.Value = NetworkManager.ConnectedClients.Count;
        }

    }

    public override void OnNetworkSpawn()
    {
        playerMove = GetComponent<Movement>();
        CharacterID.OnValueChanged += (int pre, int post) =>
        {
            InstantiateModel(post);
        };
         if (IsServer)
         {
             CharacterID.Value = NetworkManager.ConnectedClients.Count;
         }

        InstantiateModel(CharacterID.Value);
        playerResources = GetComponent<PlayerResources>();


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
            Destroy(ActiveModel.gameObject);
        }
        GameObject ModelGO = Instantiate(CharacterList.Instance.Chracters[CharacterID.Value % 2].Model,ModelAnchor);
        ModelGO.transform.SetLocalPositionAndRotation(new Vector3(0, -1, 0), Quaternion.Euler(0, 0, 0));
        ActiveModel = ModelGO.transform;
        PlayerAnimationScript = GetComponent<PlayerAnimationScript>();

        Animator animator = ModelGO.transform.GetComponentInChildren<Animator>(false);
        PlayerAnimationScript.setAnimatorField(animator);


        var combatManager = GetComponent<PlayerCombatManager>();
        if (combatManager != null)
        {
            var assignedSkill = CharacterList.Instance.Chracters[CharacterID.Value % 2].Skill1;
            combatManager.Skill1 = assignedSkill.Type switch
            {
                SkillType.EntitySpawner => new EntitySpawningSkill(),
                SkillType.Dash => null,
                SkillType.Seroid => null,
                _ => null
            };
            combatManager.Skill1.SkillDataSO = assignedSkill;
            combatManager.Skill1.animator = PlayerAnimationScript;
        }

    }

    void Update()
    {
    }
   
}
