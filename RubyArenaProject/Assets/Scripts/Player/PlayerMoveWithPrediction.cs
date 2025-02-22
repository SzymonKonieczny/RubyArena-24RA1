using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerMoveWithPrediction : NetworkBehaviour
{
    Queue<InputState> inputStateBuffer;
    void Start()
    {
        var networkTransform = GetComponent<NetworkTransform>();
        networkTransform.enabled=false;

        Debug.Log("DISABLING NETWORK TRANSFORM FOR " + this.name);
    }

    void Update()
    {
        
    }
    private void FixedUpdate()
    {
    }

    void doServerFixedUpdate()
    {

    }

    void doClientFixedUpdate()
    {

    }
}
