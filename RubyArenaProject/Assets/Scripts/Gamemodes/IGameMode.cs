using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameMode
{
    public void RegisterPlayer(ulong networkId);
    public void RegisterObject(ulong networkId);
}
