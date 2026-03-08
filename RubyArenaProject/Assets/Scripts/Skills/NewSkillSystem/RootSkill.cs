using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class RootSkill : SkillBase
{
    [SerializeField] GameObject projectile;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void OnTransformParentChanged()
    {
        Init();

    }
    // Update is called once per frame
    void Update()
    {
        if (InputCollector == null || combatManagerRef == null || isOnCooldown() || !combatManagerRef.IsOwner)
            return;

        if (spellTriggeringFlag.value && combatManagerRef.IsOwner)
        {
            Use();
        }
}

    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;

        animationScript.Trigger("WindUp");
        combatManagerRef.SetStunTimer(windupTime);
        Vector3 LookDir = getLookDirection();

        ServerSideUseServerRPC(LookDir);


        return true;
    }

    [ServerRpc]
    void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (isOnCooldown()) return;
        setCooldown(cooldown);

        StartCoroutine(SpawnEntityDelayed(lookDir));
       
    }
    IEnumerator SpawnEntityDelayed(Vector3 lookDir)
    {
        yield return new WaitForSeconds(windupTime);
        GameObject skillEntityGO = Instantiate(projectile);

        skillEntityGO.GetComponent<NetworkObject>().Spawn();
        skillEntityGO.transform.SetPositionAndRotation(combatManagerRef.SkillshotSpawnPoint.transform.position + lookDir * 2, Quaternion.LookRotation(lookDir, Vector3.up));

        ServerAnnounceSpellCastClientRPC(0);
    }
    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC(ulong networkObjId)
    {
        if (IsOwner)
        {
            animationScript.Trigger("SpellAcknowledge2");
        }
        else
        {
            animationScript.Trigger("WindUp");
            animationScript.Trigger("SpellAcknowledge2");
        }

    }

}
