namespace xFrame.Serialization
{
    /// 序列化为json
    /// 一般用于读取json配置文件
    public interface IJsonConvert
    {
        string ToJson(object obj);
        T FromJson<T>(string json);
    }
}