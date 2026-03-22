using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;


public class Minion : NetworkBehaviour
{
    public Transform target;
   [SerializeField] private NavMeshAgent agent;

    void Start()
    {
        agent.SetDestination(target.position);
    }

    void Update()
    {
        
    }
}
