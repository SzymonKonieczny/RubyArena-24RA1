using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameMode
{
    public void RegisterPlayer(long networkId);
    public void RegisterObject(long networkId);
}
