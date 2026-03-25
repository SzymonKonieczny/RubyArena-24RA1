using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MinionSpawnRequester: NetworkBehaviour
{
    public MinionSpawner spawner;
    public GameObject minionPrefab;
    public float minionSpawnTime = 2;
    private float remamningCooldown = 5;
    public BrawlGameMode.BrawlTeam targetTeam;
    BrawlGameMode gameModeManager;
    public void Initialize(  BrawlGameMode brawlGameMode)
    {
        gameModeManager = brawlGameMode;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!IsServer) return;

        var spawnerList = GameObject.FindObjectsOfType<MinionSpawner>();
        if (spawnerList.Length != 1)
            Debug.LogError("There has to be exacly one gameobject with a MinionSpawner component");
        spawner = spawnerList[0];

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;

        remamningCooldown -= Time.deltaTime;
        if( remamningCooldown < 0 )
        {
            remamningCooldown += minionSpawnTime;
            var minionTarget = gameModeManager?.getTargetBaseRef(targetTeam)?.transform;
            if (minionTarget == null)
            {
                Debug.LogError("Unable to determine the minion.target!");
            }

            var minionGO = Instantiate(minionPrefab);
            var minion = minionGO.GetComponent<Minion>();
            minion.transform.position = transform.position;
            minion.target = minionTarget;
            minion.targetTeam = targetTeam;
            spawner.SpawnMinion(minion);
        }
    }
}
