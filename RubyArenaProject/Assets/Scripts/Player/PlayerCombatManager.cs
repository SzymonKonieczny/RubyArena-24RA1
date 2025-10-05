using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEditor;
using Unity.VisualScripting;

[RequireComponent(typeof(InputCollectorScript))]


public class PlayerCombatManager : NetworkBehaviour
{
    public BaseSkill Skill1;
    public BaseSkill Skill2;
    public SkillDataSO Skill1SO;
    public SkillDataSO Skill2SO;

    public NetworkVariable<bool> canCombat = new(true);

    [SerializeField] float SkillCooldown = 0;
    InputCollectorScript InputCollector;
    PlayerScript playerScript;
    public Transform SkillshotSpawnPoint;

    public PlayerAnimationScript animationScript;

    public Movement playerMove;
    [SerializeField] NetworkObject NetworkObject;
    private bool isInitialized = false;

    [ClientRpc]
    public void SetStunTimerClientRPC(float time)
    {
        SetStunTimer(time);
    }

    public void SetStunTimer(float time)
    {
        if (InputCollector.StunTime < 0)
            InputCollector.StunTime = 0;

        InputCollector.StunTime += time;

    }
    private void OnTransformParentChanged()
    {
        Initialize();
    }
    // Start is called before the first frame update
    public void Initialize()
    {
        isInitialized = true;

        InputCollector = GetComponent<InputCollectorScript>();
        playerScript = GetComponent<PlayerScript>();
        animationScript = GetComponent<PlayerAnimationScript>();
        playerMove = GetComponent<Movement>();
        NetworkObject = GetComponent<NetworkObject>();
       

    }
    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || !isInitialized) return;
       
        SkillCooldown -= Time.deltaTime;

        if ( !canCombat.Value) return;
        // canCombat is also checked when accepting/rejecting a spell

        return;


    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("NonFightArea"))
            canCombat.Value = false;

    }
    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("NonFightArea"))
            canCombat.Value = true;
    }

}
