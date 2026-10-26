using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;

/// <summary>
/// NavMesh-based movement for mobs. Server-authoritative destination setting.
/// Position is synced via NetworkTransform component on the MobCore.
///
/// Physics setup: NavMeshAgent drives movement; Rigidbody is kinematic (for collision
/// presence and knockback support). If a Rigidbody is present, its position is synced
/// to the agent each frame so physics queries (OverlapSphere etc.) see the mob correctly.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class MobMovement : NetworkBehaviour
{
    NavMeshAgent agent;
    Rigidbody rb;

    // Knockback velocity added by external sources (e.g. skills)
    Vector3 pendingKnockback;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = true; // NavMeshAgent drives position
        }
    }

    void FixedUpdate()
    {
        if (!IsServer) return;

        // Sync Rigidbody to NavMeshAgent so physics queries are accurate.
        // This only matters if a Rigidbody exists on the same GO as the agent.
        if (rb != null && agent != null)
        {
            rb.MovePosition(transform.position);
        }

        // Apply any pending knockback
        if (pendingKnockback != Vector3.zero && agent != null)
        {
            // Temporarily disable agent, apply knockback via transform, then re-enable
            agent.enabled = false;
            transform.position += pendingKnockback * Time.fixedDeltaTime;
            pendingKnockback = Vector3.zero;
            agent.enabled = true;
            agent.Warp(transform.position);
        }
    }

    public void SetDestination(Vector3 destination)
    {
        if (agent == null || !IsServer) return;
        agent.SetDestination(destination);
    }

    public void Stop()
    {
        if (agent == null) return;
        agent.ResetPath();
    }

    public bool IsAtDestination(float tolerance = 0.5f)
    {
        if (agent == null || !agent.hasPath) return true;
        return agent.remainingDistance <= tolerance;
    }

    public bool IsMoving => agent != null && agent.hasPath && agent.remainingDistance > agent.stoppingDistance;

    public Transform Model => transform;

    /// <summary>
    /// Applies a one-shot knockback impulse. Server-only since mobs are server-owned.
    /// </summary>
    public void ApplyKnockback(Vector3 impulse)
    {
        pendingKnockback += impulse;
    }
}
