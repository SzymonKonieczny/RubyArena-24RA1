using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AutoAttackSkill : BaseSkillEntityBehaviorSkillShot
{
    // Start is called before the first frame update
    public override void Start()
    {
        base.Start();
        collider = GetComponent<BoxCollider>();
        NetworkObj = GetComponent<NetworkObject>();
        Hit.OnValueChanged = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var collider = other.GetComponent<Collider>();

        var colliderPos = new Vector3(collider.transform.position.x, 0, collider.transform.position.z);
        var Rb = collider.transform.GetComponent<Rigidbody>();
        var PlayerMove = collider.transform.GetComponent<Movement>();
        var PlayerResources = collider.transform.GetComponent<UnitResource>();
        var networkObj = collider.transform.GetComponent<NetworkObject>();


        if (Rb != null)
        {
            var thisPos = new Vector3(transform.position.x, 0, transform.position.z);

            Rb.AddForce((((colliderPos - thisPos)).normalized * 500)
                , ForceMode.Acceleration);
        }

        if(PlayerMove!=null)
        {
            //check if the network ID isnt the same as the person who cast the autoattack!
            PlayerMove.AddNetworkRbVelocityClientRPC((colliderPos - transform.position).normalized * 50);

        }

        if(PlayerResources!=null)
        {
           if(SkillDataSO.ownerId != networkObj.OwnerClientId)
               PlayerResources.damage(SkillDataSO);
        }

    }
    private void LateUpdate()
    {
        if (!IsServer) return;
        StartCoroutine(DestroyCoroutine(0.2f));
    }
    IEnumerator DestroyCoroutine(float seconds)
    {
        if (!IsServer) yield break;

        yield return new WaitForSeconds(seconds);
        NetworkObj.Despawn(true);
    }
    protected override void AffectObjectS(NetworkObject Object)
    {
        /*
        var colliders = Physics.OverlapBox(collider.bounds.size, collider.bounds.size);
        Debug.Log($"Looking for RBs");

        foreach (var Collider in colliders)
        {
             Debug.Log($"Found {colliders.Length}");

            var Rb = Collider.transform.GetComponent<Rigidbody>();
            if (Rb != null)
            {
                var colliderPos = new Vector3(Collider.transform.position.x, 0, Collider.transform.position.z);
                var thisPos = new Vector3(transform.position.x, 0, transform.position.z);

                Rb.AddForce((((colliderPos - thisPos)).normalized * 500)
                    , ForceMode.Acceleration);
                Debug.Log($"Found RB in range {Collider.name}");
            }
        }
        NetworkObj.Despawn(true);
        */
    }

    protected override void DisableColldiersSC()
    {
        collider.enabled = false;
    }

    protected override NetworkObject GetAffectedObjectS()
    {
        return null;
    }

    protected override void PlayEffectsC()
    {
      
    }


}
