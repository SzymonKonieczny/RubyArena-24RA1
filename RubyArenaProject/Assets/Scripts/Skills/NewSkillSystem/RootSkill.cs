using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RootSkill : SkillBase
{
    [SerializeField] GameObject projectile;
    Rigidbody rbTest;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    private void OnTransformParentChanged()
    {
        Init();

        rbTest = InputCollector.transform.GetComponent<Rigidbody>();
    }
    // Update is called once per frame
    void Update()
    {
        if (InputCollector == null || combatManagerRef == null || cooldown > 0 || !combatManagerRef.IsLocalPlayer)
            return;

        if (spellTriggeringFlag.value && combatManagerRef.IsLocalPlayer)
        {
            Use();
        }
}

    public override bool Use()
    {
        animationScript = combatManagerRef.animationScript;

        float windupTime = 0.2f;
        animationScript.PlayState("WindUp", windupTime);
        InputCollector.StunTime = windupTime;
        Vector3 LookDir = getLookDirection();

        ServerSideUseServerRPC(LookDir);


        return true;
    }

    [ServerRpc]
    void ServerSideUseServerRPC(Vector3 lookDir, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        GameObject skillEntityGO = Instantiate(projectile);

        skillEntityGO.GetComponent<NetworkObject>().Spawn();
        skillEntityGO.transform.SetPositionAndRotation(combatManagerRef.SkillshotSpawnPoint.transform.position + lookDir * 2, Quaternion.LookRotation(lookDir, Vector3.up));
       // var skillEntity = skillEntityGO.GetComponent<BaseSkillEntityBehavior>();
       // skillEntity.SkillDataSO = ScriptableObject.CreateInstance<SkillDataSO>();
       // skillEntity.SkillDataSO.damage = 5;

        ServerAnnounceSpellCastClientRPC(0);
    }
    async void SpawnEntityDelayed()
    {

    }
    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC(ulong networkObjId)
    {
        //if (IsServer) return;
        animationScript.PlayState("Spellcast1");
        //animationScript.Trigger("SpellCastAccepted");

    }

}
