using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class TestSkillNewSystem : SkillBase
{
    GameObject testPrefab;
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

        GameObject SkillEntity = Instantiate(testPrefab);

        SkillEntity.GetComponent<NetworkObject>().Spawn();
        SkillEntity.transform.SetPositionAndRotation(combatManagerRef.SkillshotSpawnPoint.transform.position + lookDir * 2, Quaternion.LookRotation(lookDir, Vector3.up));

    }

    [ClientRpc]
    void ServerAnnounceSpellCastClientRPC(ulong networkObjId)
    {
        if (IsServer) return;

        animationScript.PlayState("SpellCast1");

    }

    // Start is called before the first frame update
    public void Init()
    {
        this.combatManagerRef= gameObject.GetComponentInParent<PlayerCombatManager>();
        testPrefab = Resources.Load<GameObject>("Prefabs/SkillEntities/BlakeShot");
        animationScript = combatManagerRef?.animationScript;
        InputCollector = gameObject.GetComponentInParent<InputCollectorScript>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && combatManagerRef.IsLocalPlayer) 
        {
            Use();
        }
    }

    Vector3 getLookDirection()
    {
        var ray = Camera.main.ScreenPointToRay(new Vector3((float)Screen.width / 2f, (float)Screen.height / 2f));
        Vector3 SkillDir = new();
        if (Physics.Raycast(ray, out RaycastHit raycastHit))
        {
            SkillDir = (raycastHit.point - this.combatManagerRef.SkillshotSpawnPoint.position).normalized;
        }
        else
        {
            SkillDir = ray.direction;
        }
        return SkillDir;
    }
}
