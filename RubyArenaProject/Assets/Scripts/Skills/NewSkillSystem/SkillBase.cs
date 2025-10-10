using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

public enum SkillCastType
{
    Passive,
    EClick,
    QClick,
    LeftMouseClick,
}
public class BoolRefType
{
    public bool value = false;
}
public abstract class SkillBase : NetworkBehaviour
{
    public BoolRefType spellTriggeringFlag;//for instance a key stroke
    public PlayerCombatManager combatManagerRef;
    public LayerMask rayCastMask;
    public PlayerAnimationScript animationScript;



    public InputCollectorScript InputCollector;
    public SkillCastType castType;
    [SerializeField] public SkillDataSO SkillDataSO;
    [SerializeField] public float cooldown =0;
    [SerializeField] protected float windupTime =0.2f;
    [SerializeField] protected int damage =0;
    public NetworkVariable<long> nextAvaliableTicks;
    protected bool isOnCooldown() => nextAvaliableTicks.Value > DateTime.UtcNow.Ticks;
    public abstract bool Use();
    private void setTriggerFlagRef(InputCollectorScript inputCollectorScript)
    {
        if (inputCollectorScript == null)
        {
            Debug.LogWarning("inputCollectorScript was null. Cannoit set the triggering flag reference");
            return;
        }
        switch (castType)
        {
            case SkillCastType.EClick:
                spellTriggeringFlag = InputCollector.EClickRef;
                break;
            case SkillCastType.QClick:
                spellTriggeringFlag = InputCollector.QClickRef;
                break;
            case SkillCastType.LeftMouseClick:
                spellTriggeringFlag = InputCollector.leftClickRef;
                break;
            default:
                Debug.LogWarning($"{nameof(castType)} is not supported as a triggerFlag");
                spellTriggeringFlag = new BoolRefType();
                break;
        }
    }
    protected RaycastHit getRayHit()
    {
        var ray = Camera.main.ScreenPointToRay(new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f));
        Physics.Raycast(ray, out RaycastHit raycastHit, rayCastMask);
        return raycastHit;
    }
    public void setCooldown(float cooldown)
    {
        nextAvaliableTicks.Value = System.DateTime.UtcNow.AddSeconds(cooldown).Ticks;
    }
    protected Vector3 getLookDirection()
    {
        var ray = Camera.main.ScreenPointToRay(new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f));
        Vector3 SkillDir = new();
        if (Physics.Raycast(ray, out RaycastHit raycastHit, rayCastMask))
        {
            SkillDir = (raycastHit.point - this.combatManagerRef.SkillshotSpawnPoint.position).normalized;
        Debug.DrawLine(ray.origin,
            raycastHit.point, Color.red, 3f);

        }
        else
        {
            SkillDir = ray.direction;
        }

        return SkillDir;
    }
    virtual public void Init()
    {
       
        PlayerSkillHolder skillholder = gameObject.GetComponentInParent<PlayerSkillHolder>();
        this.combatManagerRef = skillholder.playerCombatManager;
        animationScript = skillholder.animationScript;
        InputCollector = skillholder.inputCollectorScript;
        setTriggerFlagRef(InputCollector);
        if (!SkillDataSO)
        {
            SkillDataSO = ScriptableObject.CreateInstance<SkillDataSO>();

        }
        SkillDataSO.damage = damage;
        SkillDataSO.castTime = windupTime;
    }

    /*      General Usage Recommendations :
         bool Use()
        {
            Play Animation For Windup

            Get Direction etc

            Send an RPC to teh server to ask for exetution.
        }

        [ServerRpc]
        void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
        {

            Execute, or scheldue (with an async for delay) execution

            Announce it was used so the clients can play the animations

        }

        [ClientRpc]
        void ServerAnnounceSpellCastClientRPC(ulong networkObjId)
        {
            animationScript.PlayState("SpellCast1");

        }*/
}
