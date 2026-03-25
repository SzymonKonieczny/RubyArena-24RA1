using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering;
using System;
public class TeamBaseBrawlGameMode : NetworkBehaviour
{
    public BrawlGameMode.BrawlTeam BrawlTeam;
    public ExposeTriggerEventScript ExposedTrigger;

    public Action<float> onHealthChanged; //for the gameMode to subscribe to upon object registration

    [SerializeField] public float baseHealth = 200;
    [SerializeField] int minionsInRange = 0;
    // Start is called before the first frame update
    void Start()
    {
        ExposedTrigger.onTriggerEnter += TriggerEnter;
        ExposedTrigger.onTriggerExit += TriggerExit;
    }

    private void TriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Minion>(out var minion))
        {
            minionsInRange += minion.targetTeam == BrawlTeam ? 1: 0;
        }
    }

    private void TriggerExit(Collider other)
    {
        if (other.TryGetComponent<Minion>(out var minion))
        {
            minionsInRange -= minion.targetTeam == BrawlTeam ? 1 : 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        baseHealth -= minionsInRange * Time.deltaTime;
        if(minionsInRange != 0)
        {
            onHealthChanged?.Invoke(baseHealth);
        }
    }
}
