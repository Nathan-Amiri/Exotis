using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RelayCore : NetworkBehaviour
{
    // The only gamescene script that contains networked logic. All
    // interactions between players occur through this class. Once a
    // player has chosen an action, RelayCore passes the details of
    // that action to each player's ExecutionCore

    // Core logic classes can only impact each other in one direction:
    // DelegationCore > RelayCore > ExecutionCore > DelegationCore

    // Assigned in inspector:
    [SerializeField] private ExecutionCore executionCore;

    [Rpc(SendTo.Server)]
    public void RelayToServerRpc(RelayPacket packet) // Called by DelegationCore
    {
        RelayToClientRpc(packet);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RelayToClientRpc(RelayPacket packet)
    {
        executionCore.ReceivePacket(packet);
    }
}

public struct RelayPacket : INetworkSerializable
{
    public int example;

    // Define struct values as serializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref example);
    }
}