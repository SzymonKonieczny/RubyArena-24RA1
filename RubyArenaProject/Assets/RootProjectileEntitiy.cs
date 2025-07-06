using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class RootProjectileEntitiy : NetworkBehaviour
{
    [SerializeField] SkillDataSO data;
    [SerializeField] float lifeTime;
    [SerializeField] float stunTime;
    bool wasHit = false;
    [SerializeField] GameObject flyingParticles;
    [SerializeField] GameObject playerHitParticles;
    [SerializeField] Rigidbody rb;
    [SerializeField] Collider collider;


    private void Update()
    {
        if(lifeTime <=0)
        {
            this.NetworkObject.Despawn();
        }
        lifeTime-= Time.deltaTime;
        if (!wasHit)
        {
            transform.position = transform.position + transform.forward.normalized * 10 * Time.deltaTime;
        }
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
            this.transform.position = playerCombatManager.transform.position + new Vector3(0,1 , 0);
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
