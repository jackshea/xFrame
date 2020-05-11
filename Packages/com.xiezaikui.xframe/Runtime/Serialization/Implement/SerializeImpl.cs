using Framework.Runtime.Serialization.Interface;
using MessagePack;

namespace Framework.Runtime.Serialization.Implement
{
    public class SerializeImpl : ISerialize
    {
        public byte[] Serialize(object obj)
        {
            return MessagePackSerializer.Serialize(obj);
        }

        public T Deserialize<T>(byte[] msg)
        {
            return MessagePackSerializer.Deserialize<T>(msg);
        }
    }
}