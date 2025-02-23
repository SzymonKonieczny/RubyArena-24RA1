using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class TestQSkill : BaseSkillEntityBehaviorSkillShot //make it into a baseSkill
{
    // Start is called before the first frame update
    [SerializeField] ParticleSystem HitParticleSystem;
    MeshRenderer Renderer;
    Rigidbody rb;
    NetworkObject HitPlayer;

    void Start()
    {
        base.Start();
        base.OnNetworkSpawn();
        Renderer = GetComponent<MeshRenderer>();
        rb = GetComponent<Rigidbody>();
        NetworkObj = GetComponent<NetworkObject>();
        collider = GetComponent<SphereCollider>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!IsServer) return;
          rb.velocity = transform.forward * 20;
        TimeAlive += Time.fixedDeltaTime;
        if (TimeAlive > 6f)
                NetworkObj.Despawn(true);


    }

    [ClientRpc]
    void DamageThePlayerClientRpc(ulong NetworkObjID)
    {

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(NetworkObjID, out NetworkObject networkObject))
        {
            if (networkObject.IsOwner)
            {
               Vector3 force =  (networkObject.transform.position - transform.position).TimesVector(new Vector3(1, 0,1).normalized)
                    * 5 + new Vector3(0,80,0);
                Debug.LogWarning($"hit + {networkObject.name}");
                networkObject.GetComponent<Movement>().AddNetworkRbForceClientRPC(force);
                // networkObject.transform.position = new Vector3(0, 5, 0);

            }
        }
    }
    IEnumerator DestroyCoroutine(float seconds)
    {
        if (!IsServer) yield break;

        yield return new WaitForSeconds(seconds);
        NetworkObj.Despawn(true);
    }

    protected override void PlayEffectsC()
    {
        Renderer.forceRenderingOff = true;
        HitParticleSystem.Emit(50);
    }

    protected override NetworkObject GetAffectedObjectS()
    {
      var Colliders = Physics.OverlapSphere(this.transform.position, 0.4f);
      var playerScript = Colliders[0]?.transform?.GetComponent<PlayerScript>();
      if (playerScript != null)
      {
        var NetworkObjHit = Colliders[0].transform.GetComponent<NetworkObject>();
            return NetworkObjHit;
      }
      return null;
    }

    protected override void AffectObjectS(NetworkObject Object)
    {
        if(Object!= null)
        {
            Debug.Log("PlayerHit");
            DamageThePlayerClientRpc(Object.NetworkObjectId);
        }
        StartCoroutine(DestroyCoroutine(2f));
    }

    protected override void DisableColldiersSC()
    {
        collider.enabled = false;
    }
}
