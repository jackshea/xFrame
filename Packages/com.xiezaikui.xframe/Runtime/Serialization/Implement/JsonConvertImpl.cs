using MessagePack;
using xFrame.Serialization;

namespace xFrame.Serialization
{
    public class JsonConvertImpl : IJsonConvert
    {
        public string ToJson(object obj)
        {
            return MessagePackSerializer.SerializeToJson(obj);
        }

        public T FromJson<T>(string json)
        {
            var bytes = MessagePackSerializer.ConvertFromJson(json);
            return MessagePackSerializer.Deserialize<T>(bytes);
        }
    }
}