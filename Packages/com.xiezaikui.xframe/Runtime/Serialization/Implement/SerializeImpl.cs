using MessagePack;
using xFrame.Serialization;

namespace xFrame.Serialization
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