using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSystemGhostObject : MonoBehaviour
{
    public Transform followedObject;
    public void Init(Transform objectToFollow)
    {
        followedObject = objectToFollow;
    }
    // Update is called once per frame
    void Update()
    {
        if (followedObject == null) return;

        Vector3 newPos = new() ;
        newPos.x = Mathf.Lerp(transform.position.x, followedObject.position.x, 0.3f);
        newPos.y = Mathf.Lerp(transform.position.y, followedObject.position.y, 0.3f);
        newPos.z = Mathf.Lerp(transform.position.z, followedObject.position.z, 0.3f);

        this.transform.position = newPos;
    }
}
