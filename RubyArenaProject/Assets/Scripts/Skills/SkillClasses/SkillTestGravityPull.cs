using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Newtonsoft.Json.Linq;

public class SkillTestGravityPull : BaseSkillEntityBehaviorSkillShot
{
    [SerializeField] ParticleSystem HitParticleSystem;
    [SerializeField] MeshRenderer Renderer;
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        base.Start();
        base.OnNetworkSpawn();
        NetworkObj = GetComponent<NetworkObject>();
        rb = GetComponent<Rigidbody>();
        //rb.isKinematic = false;
        collider = GetComponent<SphereCollider>();
        Debug.Log($"SPAWNING AT POSITION : {transform.position}");

    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (!IsServer) return;

        if (!Hit.Value)
        {
            rb.velocity = transform.forward * 6;
        }
    }
  
  
    IEnumerator PullAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        var Colliders = Physics.OverlapSphere(this.transform.position, 10);

        foreach (var Collider in Colliders)
        {
            //var Rb = Collider.transform.GetComponent<Rigidbody>();
           //if (Rb != null)
           //{
           //    Rb.AddForce(((transform.position - Collider.transform.position + new Vector3(0, 3, 0)).normalized*500 )
           //        , ForceMode.Acceleration);
           //
           //
           //    Debug.Log($"Found RB in range {Collider.name}");
           //}
            var playerMove = Collider.transform.GetComponent<Movement>();
            if (playerMove != null)
            {
                playerMove.AddNetworkRbForceClientRPC(((transform.position - Collider.transform.position + new Vector3(0, 3, 0)).normalized * 500));
            }


        }

        yield return null;
        StartCoroutine(DestroyCoroutine(2f));

    }
    IEnumerator DestroyCoroutine(float seconds)
    {
        if(!IsServer) yield break;
        yield return new WaitForSeconds(seconds);
        NetworkObj.Despawn(true);
    }

    protected override NetworkObject GetAffectedObjectS()
    {
        return null;
    }

    protected override void AffectObjectS(NetworkObject Object)
    {
        Debug.Log("Pull AffectObjs");
        StartCoroutine(PullAfterDelay(0.3f));
    }

    protected override void PlayEffectsC()
    {
        Renderer.forceRenderingOff = true;
        HitParticleSystem.Play();

    }

    protected override void DisableColldiersSC()
    {
        collider.enabled = false;
    }
}
