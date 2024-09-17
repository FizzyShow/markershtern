using Confluent.Kafka;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using OrdersService.Deserializer;
using OrdersService.Services.KafkaSettingsFolder;
using System.Net;

namespace OrdersService.Services
{
    public class KafkaProducerService
    {
        private readonly ProducerConfig _producerConfig;
        public KafkaProducerService(IOptions<KafkaSettings> config)
        {
            _producerConfig = new ProducerConfig
            {
                BootstrapServers = config.Value.BootstrapServers,
                ClientId = Dns.GetHostName()
            };
        }

        public async Task ProduceMessageAsync<T>(T message, string topic)
            where T : IMessage<T>, new()
        {
            var producer = new ProducerBuilder<Null, T>(_producerConfig);

            producer.SetValueSerializer(new ProtoSerializer<T>());
            using (var producerBuild = producer.Build())
            {
                await producerBuild.ProduceAsync(topic, new Message<Null, T>
                {
                    Value = message
                });
            }
        }
    }
}
