using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RelayCore : NetworkBehaviour
{
    //The only gamescene script that contains networked logic. All
    //player interaction occurs through this class. Once a player
    //has chosen an action, RelayCore passes the details of that
    //action to each player's ExecutionCore

    //Core logic classes can only impact each other in one direction:
    //DelegationCore > RelayCore > ExecutionCore > DelegationCore

    //assigned in inspector:
    [SerializeField] private ExecutionCore executionCore;

    [ServerRpc (RequireOwnership = false)]
    public void RelayServerRpc(RelayPacket packet) //called by DelegationCore
    {
        RelayClientRpc(packet);
    }

    [ClientRpc]
    private void RelayClientRpc(RelayPacket packet)
    {
        executionCore.ReceivePacket(packet);
    }
}

public struct RelayPacket : INetworkSerializable
{
    public int example;

    //define struct values as serializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref example);
    }
}