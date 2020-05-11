namespace Framework.Runtime.Infrastructure.Serialization
{
    public interface ISerializerStorage
    {
        void Load(ISerializerStream stream);

        void Save(ISerializerStream stream);
    }
}