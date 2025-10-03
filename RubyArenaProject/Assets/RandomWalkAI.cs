using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RandomWalkAI : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Transform border1;
    [SerializeField] Transform border2;
    [SerializeField] Vector3 target;
    // Start is called before the first frame update
    void Start()
    {
        animator.SetFloat("Velocity", 1);
        target = new Vector3(Random.Range(border1.position.x, border2.position.x), 0, Random.Range(border1.position.z, border2.position.z));
        agent.SetDestination(target);

    }

    // Update is called once per frame
    void Update()
    {
        if((transform.position - target).magnitude <2)
        {
            target = new Vector3(Random.Range(border1.position.x, border2.position.x), 0, Random.Range(border1.position.z, border2.position.z));
            agent.SetDestination(target);

        }
    }
}
