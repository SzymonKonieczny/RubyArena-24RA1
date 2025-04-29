using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering;
public class TestQSkill : BaseSkillEntityBehaviorSkillShot //make it into a baseSkill
{
    // Start is called before the first frame update
    [SerializeField] ParticleSystem HitParticleSystem;
    MeshRenderer Renderer;
    Rigidbody rb;
    NetworkObject HitPlayer;
    [SerializeField] int speed = 60;

    void Start()
    {
        base.Start();
        base.OnNetworkSpawn();
        Renderer = GetComponent<MeshRenderer>();
        rb = GetComponent<Rigidbody>();
        NetworkObj = GetComponent<NetworkObject>();
        collider = GetComponent<Collider>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (isActive)
            transform.Translate(transform.forward * speed * Time.deltaTime,Space.World);

        if (!IsServer) return;


        TimeAlive += Time.fixedDeltaTime;
        if (TimeAlive > 6f)
                NetworkObj.Despawn(true);


    }

    [ClientRpc]
    void MovePlayerAroundClientRPC(ulong networkObjectID)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectID, out NetworkObject networkObject))
        {
            if (networkObject.IsOwner)
            {
               Vector3 force =  (networkObject.transform.position - transform.position).ScaleVec(new Vector3(1, 0,1).normalized)
                    * 5;
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
     // var Colliders = Physics.OverlapSphere(this.transform.position, 0.4f);

      var playerScript = collisioninfo.transform.GetComponent<PlayerScript>(); //Colliders[0]?.transform?.GetComponent<PlayerScript>();
      if (playerScript != null)
      {
        var NetworkObjHit = playerScript.transform.GetComponent<NetworkObject>();
            return NetworkObjHit;
      }
      return null;
    }

    protected override void AffectObjectS(NetworkObject Object)
    {
        if(Object!= null)
        {
            Debug.Log("PlayerHit");
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(Object.NetworkObjectId, out NetworkObject networkObject))
            {
                MovePlayerAroundClientRPC(networkObject.NetworkObjectId);
                var playerRes =  networkObject.GetComponent<PlayerResources>();
                playerRes.damage(SkillDataSO);
            }

        }
        StartCoroutine(DestroyCoroutine(2f));
    }

    protected override void DisableColldiersSC()
    {
        collider.enabled = false;
        isActive = false;
    }
}
