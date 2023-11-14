/*
guide for netcode rpc serialization

guide covers the serialization of:
1. network behaviours
2. network behaviour arrays (unlike string arrays, networkBehaviourReference arrays are serializable) 
3. unserializable arrays (such as string arrays)
4. structs with serializable variable types
5. structs with unserializable variable types

(arrays of ints/floats/bools, vectors, quaternions, etc. are already serializable)


using Unity.Netcode;
using UnityEngine;

public class SerializationGuide : NetworkBehaviour
{
    private void Start()
    {
        NetworkManager.StartHost();
    }

    //1 & 5
    [SerializeField] private TestBehaviour assignedTestBehaviour;

    //3 & 5
    private readonly string[] myStrings = new string[] { "banana", "apple", "strawberry" };

    public override void OnNetworkSpawn()
    {
        //1
        BehaviourServerRpc(assignedTestBehaviour);

        //2
        NetworkBehaviourReference[] testBehaviours = new NetworkBehaviourReference[]
        {
            assignedTestBehaviour,
            assignedTestBehaviour
        };
        BehaviourArrayServerRpc(testBehaviours);

        //3
        StringContainer[] step3StringContainers = new StringContainer[myStrings.Length];
        for (int i = 0; i < step3StringContainers.Length; i++)
        {
            step3StringContainers[i] = new()
            {
                containedString = myStrings[i]
            };
        }
        StringArrayServerRpc(step3StringContainers);

        //4
        SimpleStruct simpleStruct = new()
        {
            myInt = 57
        };
        SimpleStructServerRpc(simpleStruct);

        //5
        StringContainer[] step5StringContainers = new StringContainer[myStrings.Length];
        for (int i = 0; i < step5StringContainers.Length; i++)
        {
            step5StringContainers[i] = new()
            {
                containedString = myStrings[i]
            };
        }
        ComplexStruct complexStruct = new()
        {
            testBehaviour = assignedTestBehaviour,
            myStringContainers = step5StringContainers
        };
        ComplexStructServerRpc(complexStruct);
    }

    //1
    [ServerRpc]
    private void BehaviourServerRpc(NetworkBehaviourReference reference)
    {
        reference.TryGet(out TestBehaviour testBehaviour);
        Debug.Log(testBehaviour.testVariable);
    }

    //2
    [ServerRpc]
    private void BehaviourArrayServerRpc(NetworkBehaviourReference[] references)
    {
        foreach (NetworkBehaviourReference reference in references)
        {
            reference.TryGet(out TestBehaviour testBehaviour);
            Debug.Log(testBehaviour.testVariable);
        }
    }

    //3
    [ServerRpc]
    private void StringArrayServerRpc(StringContainer[] newStringContainers)
    {
        foreach (StringContainer stringContainer in newStringContainers)
            Debug.Log(stringContainer.containedString);
    }

    //4
    [ServerRpc]
    private void SimpleStructServerRpc(SimpleStruct simpleStruct)
    {
        Debug.Log(simpleStruct.myInt);
    }

    //5
    [ServerRpc]
    private void ComplexStructServerRpc(ComplexStruct complexStruct)
    {
        complexStruct.testBehaviour.TryGet(out TestBehaviour testBehaviour);
        Debug.Log(testBehaviour.testVariable);

        foreach (StringContainer stringContainer in complexStruct.myStringContainers)
            Debug.Log(stringContainer.containedString);
    }
}

//3 & 5
public class StringContainer : INetworkSerializable
{
    public string containedString;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        if (serializer.IsWriter)
            serializer.GetFastBufferWriter().WriteValueSafe(containedString);
        else
            serializer.GetFastBufferReader().ReadValueSafe(out containedString);
    }
}

//4
public struct SimpleStruct : INetworkSerializable
{
    public int myInt;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref myInt);
    }
}

//5
public struct ComplexStruct : INetworkSerializable
{
    public NetworkBehaviourReference testBehaviour;
    public StringContainer[] myStringContainers;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref testBehaviour);
        serializer.SerializeValue(ref myStringContainers);
    }
}
*/