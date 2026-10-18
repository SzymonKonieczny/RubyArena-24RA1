using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(InputCollectorScript))]
public class PlayerCombatManager : CombatEntity
{
    [SerializeField] private PlayerAnimationScript animationScript;
    [SerializeField] private Movement playerMove;

    private InputCollectorScript inputCollector;

    public override bool CanCombat { get; protected set; } = true;
    public override PlayerAnimationScript AnimationScript => animationScript;
    public override Movement PlayerMove => playerMove;

    public override void SetStunTimer(float time)
    {
        if (inputCollector == null) 
        {
            Debug.LogWarning($"No Input Collector Found for {this.name}");
            return;
        }


        if(IsOwner)
        {
            // Logic doubling, its bad i know. Without this appraoch if a client calls ClientRPC it will ignore the call. Nothing will happen and the "windup" animation self-stuns wont apply
            if (inputCollector.StunTime < 0)
                inputCollector.StunTime = 0;
            inputCollector.StunTime += time;

        }
        else
        {
            SetStunTimerClientRpc(time);
        }

  
    }

    [ClientRpc]
    void SetStunTimerClientRpc(float time)
    {
        if (inputCollector == null) 
        {
            Debug.LogWarning($"No Input Collector Found for {this.name}");
            return;
        }

        if (inputCollector.StunTime < 0)
            inputCollector.StunTime = 0;

        inputCollector.StunTime += time;
        Debug.LogWarning($"Called Stun for duration: {time}!");
    }

    private void OnTransformParentChanged()
    {
        Initialize();
    }

    public void Initialize()
    {
        inputCollector = GetComponent<InputCollectorScript>();
        animationScript = GetComponent<PlayerAnimationScript>();
        playerMove = GetComponent<Movement>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("NonFightArea"))
            CanCombat = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("NonFightArea"))
            CanCombat = true;
    }
}
