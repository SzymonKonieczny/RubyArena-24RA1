using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Cinemachine;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.SceneManagement;

public class PlayerScript : NetworkBehaviour
{
    [SerializeField] Transform[] CameraTransforms;
    public Transform ModelAnchor;

    [SerializeField] PlayerAnimationScript PlayerAnimationScript;
    [SerializeField] PlayerCombatManager playerCombatManager;
    public Transform ActiveModel;
    public PlayerResources playerResources;
    public Movement playerMove;
    private PlayerSkillHolder skillHolder;
    public NetworkVariable<int> characterID = new(0);
    public List<NetworkObject> spawnedObjects = new();

    private void Start()
    {
    }
    public override void OnNetworkSpawn()
    {
        InitializePlayer();
    }
    void InitializePlayer()
    {

        playerMove = GetComponent<Movement>();
        playerCombatManager = GetComponent<PlayerCombatManager>();
        playerResources = GetComponent<PlayerResources>();
        skillHolder = GetComponent<PlayerSkillHolder>();
        InitializeCharacter();

        if (!IsOwner)
        {
            foreach (Transform t in CameraTransforms)
            {
                Destroy(t.GetComponent<CinemachineFreeLook>());
            }
        }
        else // if we are the local player
        {
            PersistentCanvasManger.Instance.playerScript = this;
            LocalPlayerStateManager.LocalInstance.localPlayerRef = skillHolder;
            LocalPlayerStateManager.LocalInstance.localPlayerInitialized?.Invoke();
        }
        playerCombatManager.Initialize();
        playerResources.Initialize();
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += onLoadComplete;
    }    
    void onLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {

        GameObject GM = GameObject.FindWithTag("GameModeManager");
        if (GM == null) return;

        IGameMode GameMode = GM.GetComponent<IGameMode>();
        GameMode.RegisterPlayer(NetworkObject.NetworkObjectId);
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= onLoadComplete;
    }
    void InitializeCharacter()
    {
        int CharacterID = this.characterID.Value;

        if (CharacterID < 0 || CharacterID >= CharacterList.Instance.Characters.Count)
            return; //This means they dont have a character (post-lobby jointer potentially)
        if (ActiveModel != null)
        {
            Destroy(ActiveModel.gameObject);
        }
        GameObject ModelGO = Instantiate(CharacterList.Instance.Characters[CharacterID].characterModel.Model,ModelAnchor);

        ModelGO.transform.SetLocalPositionAndRotation(new Vector3(0, -1, 0), Quaternion.Euler(0, 0, 0));
        ActiveModel = ModelGO.transform;
        PlayerAnimationScript = GetComponent<PlayerAnimationScript>();

        Animator animator = ModelGO.transform.GetComponentInChildren<Animator>(false);
        PlayerAnimationScript.setAnimatorField(animator);


        var combatManager = GetComponent<PlayerCombatManager>();
        if (combatManager != null)
        {
            if(IsServer)
            {
                addSkillPrefab(CharacterList.Instance.Characters[CharacterID ].characterModel.SkillPrefab1, NetworkObject.OwnerClientId);
                addSkillPrefab(CharacterList.Instance.Characters[CharacterID ].characterModel.SkillPrefab2, NetworkObject.OwnerClientId);
                var autoAttackCarrierGO = addSkillPrefab(CharacterList.Instance.Characters[CharacterID ].characterModel.AutoAttack, NetworkObject.OwnerClientId);
                if (autoAttackCarrierGO != null)
                {
                    AutoAttackSkillCarrier autoAttackScript = autoAttackCarrierGO.GetComponent<AutoAttackSkillCarrier>();
                    autoAttackScript.autoAttackParams.Value = CharacterList.Instance.Characters[CharacterID].characterModel.AutoAttackParams;
                    autoAttackScript.Init();
                }
            }
        }

    }

    GameObject addSkillPrefab(GameObject prefab, ulong clientID)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Null skill prefab. Couldnt instantiate the carrier");
            return null;
        }
            var skillPrefab = prefab;

        var skillHolder = GetComponent<PlayerSkillHolder>().transform;

        GameObject skillpref = Instantiate(skillPrefab);
        var skillprefNetworkObj = skillpref.GetComponent<NetworkObject>();
        spawnedObjects.Add(skillprefNetworkObj);
        skillprefNetworkObj.SpawnWithOwnership(clientID);
        skillprefNetworkObj.TrySetParent(skillHolder,false);
        return skillpref;
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!IsServer) return;

        foreach (var obj in spawnedObjects)
        {
            if(obj!=null && obj.IsSpawned)
            {
                obj.Despawn();
            }
        }
    }

    void Update()
    {
    }
   
}
