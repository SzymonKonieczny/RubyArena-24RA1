using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameMode
{
    public bool CanDamage(ulong networkId);
    public void RegisterPlayer(ulong networkId);
    public void RegisterObject(ulong networkId);
}
