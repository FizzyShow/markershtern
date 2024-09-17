using Confluent.Kafka;
using Google.Protobuf;

namespace OrdersService.Deserializer
{
    public class ProtoSerializer<T> : ISerializer<T> where T : IMessage
    {
        public byte[] Serialize(T data, SerializationContext context)
        {
            return ((IMessage)data).ToByteArray();
        }
    }
}
