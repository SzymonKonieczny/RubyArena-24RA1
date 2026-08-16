using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEditor;
using Unity.VisualScripting;

[RequireComponent(typeof(InputCollectorScript))]


public class PlayerCombatManager : NetworkBehaviour
{
    public NetworkVariable<bool> canCombat = new(true);

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

    void FixedUpdate()
    {

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
