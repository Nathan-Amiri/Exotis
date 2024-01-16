using Unity.Netcode;
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
public static class StringContainerConverter
{
    public static StringContainer[] ContainStrings(string[] array)
    {
        // Place each of an array's strings into a serializable container, then return the array of containers

        StringContainer[] stringContainers = new StringContainer[array.Length];
        for (int i = 0; i < stringContainers.Length; i++)
        {
            stringContainers[i] = new()
            {
                containedString = array[i]
            };
        }

        return stringContainers;
    }
}