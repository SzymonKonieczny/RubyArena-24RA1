using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;


/// <summary>
/// Base class for the NetworkBehavior sitting on skill living inside the world (the skill entity)
/// such as for instance a Fireball traveling thru the world or
/// a field of AOE damage laying on the ground
/// </summary>
public abstract class BaseSkillEntityBehavior : NetworkBehaviour
{
   [SerializeField] protected NetworkVariable<bool> Hit = new NetworkVariable<bool>(false);
    [SerializeField] protected bool isActive = true;
    [SerializeField] protected bool wasCancelled = false;
    [SerializeField] protected float TimeAlive = 0;
    [SerializeField] protected GameObject entityToSpawn; //Entity to to spawn (for instance after collition for AOE overtime effects)
    [SerializeField] protected Collider collider;

    [SerializeField] protected NetworkObject NetworkObj;
    [SerializeField] public SkillDataSO SkillDataSO;
    public ulong ownerNetworkObjectId;
    protected Collision collisioninfo;
    public virtual void Start()
    {
    }
    protected virtual void CollisionRegisteredOnServer(bool prev, bool newV)
    { }
    protected abstract void PlayEffectsC();

    /// <summary>
    /// If you need to disable colliders, do it here
    /// </summary>
    protected abstract void DisableColldiersSC();
    protected void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;
        
        collisioninfo = collision;
        Hit.Value = true;
        

    }
    protected void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;


        if (other.CompareTag("NonFightArea"))
        {
            wasCancelled = true;
            Hit.Value = true;
        }
    }



}
