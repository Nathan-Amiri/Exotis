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

    // SCENE REFERENCE:
    [SerializeField] private ExecutionCore executionCore;

    public void PrepareToRelayPacket(RelayPacket packet)
    {
        // This method acts as a safety net to prevent serialization errors
        // (Netcode cannot serialize a null string or array)

        packet.actionType ??= string.Empty;
        packet.name ??= string.Empty;
        packet.hexType ??= string.Empty;
        packet.targetSlots ??= new int[] { };

        RelayToServerRpc(packet);
    }

    [Rpc(SendTo.Server)]
    private void RelayToServerRpc(RelayPacket packet) // Called by DelegationCore
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
    public int player;
    public string actionType; // Possible values: pass, repopulate, trait, retreat, gem, spark, spell (recast?)
    // Only above variables used for pass

    public int casterSlot;
    // Only above variables used for repopulate

    public int[] targetSlots; // In the order they were selected
    // Only above variables used for trait, retreat, gem, spark

    // Below variables only used for Spells
    public string name;
    public int wildTimeScale;
    public string hexType; // Possible values: slow, poison, weaken
    public bool potion;
    public bool frenzy;

    // Define struct values as serializable
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref player);
        serializer.SerializeValue(ref actionType);
        serializer.SerializeValue(ref casterSlot);
        serializer.SerializeValue(ref targetSlots);
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref wildTimeScale);
        serializer.SerializeValue(ref hexType);
        serializer.SerializeValue(ref potion);
        serializer.SerializeValue(ref frenzy);
    }
}