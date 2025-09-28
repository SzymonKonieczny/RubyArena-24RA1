using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class RootProjectileEntitiy : NetworkBehaviour
{
    [SerializeField] SkillDataSO data;
    [SerializeField] float lifeTime;
    [SerializeField] float stunTime;
    [SerializeField] float speed;
    bool wasHit = false;
    [SerializeField] GameObject flyingParticles;
    [SerializeField] GameObject playerHitParticles;
    [SerializeField] Rigidbody rb;
    [SerializeField] Collider collider;
    [SerializeField] GameObject ParticleHolderGhostObject;
    ParticleSystemGhostObject particleSystemGhostObject;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsClient)
        {
            GameObject particleHolder =Instantiate(ParticleHolderGhostObject);
            particleSystemGhostObject = particleHolder.GetComponent<ParticleSystemGhostObject>();
            if(particleSystemGhostObject!=null)
            {
                particleSystemGhostObject.Init(this.transform);
                particleSystemGhostObject.transform.position = this.transform.position;
                var particleSystems = particleSystemGhostObject.GetComponentsInChildren<ParticleSystem>(true);
                foreach(var PS in particleSystems)
                {
                    if (PS.transform.name == "FlyingPaticles")
                    {
                        flyingParticles = PS.gameObject;
                    }
                    else if (PS.transform.name == "PrisonParticles")
                    {
                        playerHitParticles = PS.gameObject;
                    }
                }

                flyingParticles.SetActive(true);
            }
        }
    }
    private void FixedUpdate()
    {

        if (!IsServer) return;
        if(lifeTime <=0)
        {
            this.NetworkObject.Despawn();
        }
        lifeTime-= Time.deltaTime;
        if (!wasHit)
        {
            transform.position = transform.position + transform.forward.normalized * speed * Time.fixedDeltaTime;
        }
    }
    public override void OnNetworkDespawn()
    {
        Destroy(particleSystemGhostObject.gameObject);

        base.OnNetworkDespawn();
    }
    [ClientRpc]
    public void SwapParticleSystemsClientRPC()
    {
        flyingParticles.SetActive(false);
        playerHitParticles.SetActive(true);
    }
    IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(stunTime);
        this.NetworkObject.Despawn();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            var playerNO = collision.transform.GetComponent<NetworkObject>();
            var playerCombatManager = collision.transform.GetComponent<PlayerCombatManager>();
            rb.velocity = Vector3.zero;
            collider.enabled = false;
            this.transform.rotation = Quaternion.Euler(180, 0, 0);
            if (playerCombatManager == null || playerNO == null) 
            {   // JUST FOR TESTING WITHOUT 2 PLAERS, CONTENT OF THE IF REMOVABLE, JUST KEEP THE "RETURN"
                lifeTime = stunTime;
                wasHit = true;
                StartCoroutine(DespawnAfterDelay());
                SwapParticleSystemsClientRPC();
                this.transform.position = collision.transform.position + new Vector3(0, 1, 0);
                return;
            }
            playerCombatManager.SetStunTimerClientRPC(stunTime);
            SwapParticleSystemsClientRPC();
            this.transform.position = playerCombatManager.transform.position + new Vector3(0,-1.0f , 0);
            lifeTime = stunTime;
            wasHit = true;
            StartCoroutine(DespawnAfterDelay());
        }
        else
        {
            this.NetworkObject.Despawn();
        }
    }
}
