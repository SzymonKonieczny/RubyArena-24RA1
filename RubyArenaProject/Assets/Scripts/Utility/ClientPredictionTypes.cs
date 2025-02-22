using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
public struct InputState : INetworkSerializable
{
    float MoveVecX;
    float MoveVecY;
    float TimeStamp;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref MoveVecX);
        serializer.SerializeValue(ref MoveVecY);

    }
}
