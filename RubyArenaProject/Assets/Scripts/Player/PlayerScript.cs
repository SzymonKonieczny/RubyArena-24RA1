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
    public NetworkVariable<int> CharacterID= new NetworkVariable<int>(-1); 
    public NetworkVariable<bool> hasCharacter = new(false);

    [SerializeField] PlayerAnimationScript PlayerAnimationScript;
    [SerializeField] PlayerCombatManager playerCombatManager;
    public Transform ActiveModel;
    public PlayerResources playerResources;
    public Movement playerMove;


    public override void OnNetworkSpawn()
    {
        playerMove = GetComponent<Movement>();
        playerCombatManager = GetComponent<PlayerCombatManager>();
        transform.position = new Vector3(25,20,12);
        CharacterID.OnValueChanged += (int pre, int post) =>
        {
            InitializeCharacter();
        };
        if (IsServer)
        {
            CharacterID.Value = -1;
        }

        playerResources = GetComponent<PlayerResources>();


        if (!IsLocalPlayer)
        {
            InitializeCharacter(); //for every character already in the game, try to initialize their characters 
            foreach (Transform t in CameraTransforms)
            {
                Destroy(t.GetComponent<CinemachineFreeLook>());
            }
        }
        else // if we are the local player
        {
            CanvasManger.Instance.CharacterSelect.gameObject.SetActive(true);
            CanvasManger.Instance.playerScript = this;
            playerResources.Initialize();
            playerCombatManager.Initialize();
        }
        isStunnedNetworkVar.Value = true; //We start stunned for as long as we are choosing our champ


    }
    void InitializeCharacter()
    {
        if (CharacterID.Value == -1) //means character wasnt yet selected
            return;

        if(IsLocalPlayer) //Only when I have selected a champ, my local UI changes!
            CanvasManger.Instance.CharacterSelect.gameObject.SetActive(false);


        if (ActiveModel != null)
        {
            Destroy(ActiveModel.gameObject);
        }
        GameObject ModelGO = Instantiate(CharacterList.Instance.Chracters[CharacterID.Value % 2].Model,ModelAnchor);
        hasCharacter.Value = true;

        ModelGO.transform.SetLocalPositionAndRotation(new Vector3(0, -1, 0), Quaternion.Euler(0, 0, 0));
        ActiveModel = ModelGO.transform;
        PlayerAnimationScript = GetComponent<PlayerAnimationScript>();

        Animator animator = ModelGO.transform.GetComponentInChildren<Animator>(false);
        PlayerAnimationScript.setAnimatorField(animator);


        var combatManager = GetComponent<PlayerCombatManager>();
        if (combatManager != null)
        {
            //var assignedSkill = CharacterList.Instance.Chracters[CharacterID.Value % 2].Skill1;

            //combatManager.Skill1 = assignedSkill.Type switch
            //{
            //    SkillType.EntitySpawner => new EntitySpawningSkill(),
            //    SkillType.Dash => null,
            //    SkillType.Seroid => null,
            //    _ => null
            //};
           // combatManager.Skill1.SkillDataSO = assignedSkill;
            //combatManager.Skill1.animator = PlayerAnimationScript;

            if(IsServer)
            {

                addSkillPrefab(CharacterList.Instance.Chracters[CharacterID.Value % 2].SkillPrefab1, NetworkObject.OwnerClientId);
                addSkillPrefab(CharacterList.Instance.Chracters[CharacterID.Value % 2].SkillPrefab2, NetworkObject.OwnerClientId);

                // var skillPrefab = CharacterList.Instance.Chracters[CharacterID.Value % 2].SkillPrefab1;
                //
                // var skillHolder = GetComponent<PlayerSkillHolder>().transform;
                //
                // GameObject skillpref = Instantiate(skillPrefab);
                // var skillprefNetworkObj = skillpref.GetComponent<NetworkObject>();
                // skillprefNetworkObj.SpawnWithOwnership(NetworkObject.OwnerClientId);
                // skillprefNetworkObj.TrySetParent(skillHolder);


                // var das = skillpref.GetComponent<TestSkillNewSystem>();
            }
        }

    }

    void addSkillPrefab(GameObject prefab, ulong clientID)
    {
        if (prefab == null) return;
        var skillPrefab = prefab;

        var skillHolder = GetComponent<PlayerSkillHolder>().transform;

        GameObject skillpref = Instantiate(skillPrefab);
        var skillprefNetworkObj = skillpref.GetComponent<NetworkObject>();
        skillprefNetworkObj.SpawnWithOwnership(clientID);
        skillprefNetworkObj.TrySetParent(skillHolder);
    }
    [ServerRpc]
   public void AskToSelectCharacterServerRpc(int selectedCharacterID)
    {
        if (!IsServer || hasCharacter.Value) return;

        CharacterID.Value = selectedCharacterID;
        isStunnedNetworkVar.Value = false; //After character selection we un-stun the player

    }

    void Update()
    {
    }
   
}
