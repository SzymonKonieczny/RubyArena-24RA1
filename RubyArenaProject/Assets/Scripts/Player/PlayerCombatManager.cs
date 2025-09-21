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
       /*Skill1 = Skill1SO.Type switch
        {
            SkillType.EntitySpawner => new EntitySpawningSkill(),
            SkillType.Dash => null,
            SkillType.Seroid => null,
            _ => null
        };
        Skill1.SkillDataSO = Skill1SO;
        Skill1.animator = animationScript;

        Skill2 = Skill2SO.Type switch
        {
            SkillType.EntitySpawner => new EntitySpawningSkill(),
            SkillType.Dash => null,
            SkillType.Seroid => null,
            _ => null
        };
        Skill2.SkillDataSO = Skill2SO;
        Skill2.animator = animationScript;*/

    }

   /* private void OnDrawGizmos()
    {
        var ray = Camera.main.ScreenPointToRay(new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f));
        Physics.Raycast(ray, out RaycastHit raycastHit);
        Vector3 SkillDir =  (raycastHit.point- ray.origin).normalized; 
        ray.direction = SkillDir;
        Gizmos.color=Color.black;
        Gizmos.DrawLine(ray.origin, raycastHit.point);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(ray.origin, raycastHit.point);


        Gizmos.color = Color.blue;
        Gizmos.DrawRay(ray.origin, SkillDir);
    }*/
    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || !isInitialized) return;
       
        SkillCooldown -= Time.deltaTime;

        if (playerScript.isStunnedNetworkVar.Value || !canCombat.Value) return;
        // canCombat is also checked when accepting/rejecting a spell

        return;
/*
        // Old system
        var ray = Camera.main.ScreenPointToRay(new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f));
        Vector3 SkillDir = new();
        if (Physics.Raycast(ray, out RaycastHit raycastHit))
        {
            SkillDir  =   (raycastHit.point - SkillshotSpawnPoint.position).normalized;
        }
        else
        {
            SkillDir = ray.direction;
        }

        if (InputCollector.QClick && Skill1.CanCast())
        {
            Skill1.ChangeSkilRequestState(SkillRequestState.Requested);
            SpawnSkillEntityServerRPC(1, SkillshotSpawnPoint.position, SkillDir);
        }
        if (InputCollector.EClick && Skill2.CanCast())
        {
            Skill2.ChangeSkilRequestState(SkillRequestState.Requested);
            SpawnSkillEntityServerRPC(2, SkillshotSpawnPoint.position, SkillDir);
        }*/

    }

    [ClientRpc]
    void RequestedSpellAnswerClientRPC(bool wasAccepted, int skillID)
    {
        if (!IsOwner) return; //Only the requestee
        var RequestedSpell = skillID switch
        {
            1 => Skill1,
            2 => Skill2,
            _ => null
        };
        if (!wasAccepted)
        {
            RequestedSpell.ChangeSkilRequestState(SkillRequestState.NotRequested);
            return;
        }

        RequestedSpell.ChangeSkilRequestState(SkillRequestState.Accepted);

        SetStunTimer(RequestedSpell.SkillDataSO.castTime);
        Debug.Log($"Stunning for {InputCollector.StunTime}");
    }

    [Rpc(SendTo.ClientsAndHost)]
    void TellClientsToAnimateSkillClientRPC(int skillId)
    {
        if (IsOwner) return; //All clients other than the requestee, who need to be notified of 
        //a spell being cast
        var RequestedSpell = skillId switch
        {
            1 => Skill1,
            2 => Skill2,
            _ => null
        };
        RequestedSpell.ChangeSkilRequestState(SkillRequestState.Accepted);

    }
    [Rpc(SendTo.Server)]
    void SpawnSkillEntityServerRPC(int id, Vector3 Position, Vector3 Direction)
    {
        if (!IsServer) return;

        if (!canCombat.Value)
        {
            RequestedSpellAnswerClientRPC(false, id);
        }
        else
        {
            RequestedSpellAnswerClientRPC(true, id);
        }

        var RequestedSpell = id switch 
        {
           1 => Skill1,
           2 => Skill2,
            _ => null
        };

        
        if (RequestedSpell == null) return;

        switch (RequestedSpell.SkillDataSO.Type)
        {
            case SkillType.EntitySpawner:
                StartCoroutine(SpawnEntityAfterDelay(RequestedSpell,
                    Position, Direction));

                break;
            case SkillType.Seroid:
                break;
            case SkillType.Dash:
                break;
        }

        TellClientsToAnimateSkillClientRPC(id);

        //EditorApplication.isPaused = true;

        SkillManagerScript.SkillList[0].Invoke(1);
    }

    IEnumerator SpawnEntityAfterDelay( BaseSkill RequestedSpell,
        Vector3 Position, Vector3 Direction)
    {
        yield return new WaitForSeconds(RequestedSpell.SkillDataSO.castTime);
        GameObject SkillEntity = Instantiate(RequestedSpell.SkillDataSO.SkillEntityToSpawn);

        SkillEntity.GetComponent<NetworkObject>().Spawn();
        SkillEntity.transform.SetPositionAndRotation(Position + Direction*2, Quaternion.LookRotation(Direction,Vector3.up));

        var baseSkillEntityBehacvior = SkillEntity.GetComponent<BaseSkillEntityBehavior>();
        baseSkillEntityBehacvior.SkillDataSO = RequestedSpell.SkillDataSO;
        baseSkillEntityBehacvior.SkillDataSO.ownerId = NetworkObject.OwnerClientId;

        // EditorApplication.isPaused = true;
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
