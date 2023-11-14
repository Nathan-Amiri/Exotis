//guide for netcode rpc serialization

//guide covers the serialization of:
//1. network behaviours
//2. unsupported arrays (such as string arrays)
//3. structs with serializable variable types
//4. structs with unserializable variable types

//(arrays of ints/floats/bools, vectors, quaternions, etc. are already serializable)


//using Unity.Netcode;
//using UnityEngine;

//public class SerializationTest : NetworkBehaviour
//{
//    private void Start()
//    {
//        NetworkManager.StartHost();
//    }

//    //1 & 4
//    [SerializeField] private TestBehaviour assignedTestBehaviour;

//    //2 & 4
//    private readonly string[] myStrings = new string[] { "banana", "apple", "strawberry" };

//    public override void OnNetworkSpawn()
//    {
//        //1
//        BehaviourServerRpc(assignedTestBehaviour);

//        //2
//        StringContainer[] step2StringContainers = new StringContainer[myStrings.Length];
//        for (int i = 0; i < step2StringContainers.Length; i++)
//        {
//            step2StringContainers[i] = new()
//            {
//                containedString = myStrings[i]
//            };
//        }
//        StringArrayServerRpc(step2StringContainers);

//        //3
//        SimpleStruct simpleStruct = new()
//        {
//            myInt = 57
//        };
//        SimpleStructServerRpc(simpleStruct);

//        //4
//        StringContainer[] step4StringContainers = new StringContainer[myStrings.Length];
//        for (int i = 0; i < step4StringContainers.Length; i++)
//        {
//            step4StringContainers[i] = new()
//            {
//                containedString = myStrings[i]
//            };
//        }
//        ComplexStruct complexStruct = new()
//        {
//            testBehaviour = assignedTestBehaviour,
//            myStringContainers = step4StringContainers
//        };
//        ComplexStructServerRpc(complexStruct);
//    }

//    //1
//    [ServerRpc]
//    private void BehaviourServerRpc(NetworkBehaviourReference reference)
//    {
//        reference.TryGet(out TestBehaviour testBehaviour);
//        Debug.Log(testBehaviour.testVariable);
//    }

//    //2
//    [ServerRpc]
//    private void StringArrayServerRpc(StringContainer[] newStringContainers)
//    {
//        foreach (StringContainer stringContainer in newStringContainers)
//            Debug.Log(stringContainer.containedString);
//    }

//    //3
//    [ServerRpc]
//    private void SimpleStructServerRpc(SimpleStruct simpleStruct)
//    {
//        Debug.Log(simpleStruct.myInt);
//    }

//    //4
//    [ServerRpc]
//    private void ComplexStructServerRpc(ComplexStruct complexStruct)
//    {
//        complexStruct.testBehaviour.TryGet(out TestBehaviour testBehaviour);
//        Debug.Log(testBehaviour.testVariable);

//        foreach (StringContainer stringContainer in complexStruct.myStringContainers)
//            Debug.Log(stringContainer.containedString);
//    }
//}

////2 & 4
//public class StringContainer : INetworkSerializable
//{
//    public string containedString;
//    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
//    {
//        if (serializer.IsWriter)
//            serializer.GetFastBufferWriter().WriteValueSafe(containedString);
//        else
//            serializer.GetFastBufferReader().ReadValueSafe(out containedString);
//    }
//}

////3
//public struct SimpleStruct : INetworkSerializable
//{
//    public int myInt;

//    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
//    {
//        serializer.SerializeValue(ref myInt);
//    }
//}

////4
//public struct ComplexStruct : INetworkSerializable
//{
//    public NetworkBehaviourReference testBehaviour;
//    public StringContainer[] myStringContainers;

//    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
//    {
//        serializer.SerializeValue(ref testBehaviour);
//        serializer.SerializeValue(ref myStringContainers);
//    }
//}