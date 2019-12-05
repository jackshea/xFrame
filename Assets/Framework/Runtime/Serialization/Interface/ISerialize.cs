namespace Framework.Runtime.Serialization.Interface
{
    /// 序列化为二进制
    /// 一般序列化用于发送网络消息
    public interface ISerialize
    {
        byte[] Serialize(object obj);
        T Deserialize<T>(byte[] msg);
    }
}