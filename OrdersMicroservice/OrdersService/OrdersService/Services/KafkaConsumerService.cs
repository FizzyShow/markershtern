using Confluent.Kafka;
using Microsoft.Extensions.Options;
using OrdersService.Data.Repository;
using OrdersService.Deserializer;
using OrdersService.Models;
using OrdersService.Protos;
using OrdersService.Services.KafkaSettingsFolder;

namespace OrdersService.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IMongoRepository<ActiveBuyOrder> _orderBuyRepository;
        private readonly IMongoRepository<ActiveSellOrder> _orderSellRepository;
        private ILogger<KafkaConsumerService> _logger;
        private readonly ConsumerConfig _config;
        private readonly OrderOperationService _service;

        public KafkaConsumerService(IMongoRepository<ActiveBuyOrder> orderBuyRepository,
                                    IMongoRepository<ActiveSellOrder> orderSellRepository,
                                    IOptions<KafkaSettings> settings,
                                    ILogger<KafkaConsumerService> logger,
                                    OrderOperationService service)
        { 
            _orderBuyRepository = orderBuyRepository;
            _orderSellRepository = orderSellRepository;
            _logger = logger;
            _service = service;

            _config = new ConsumerConfig
            {
                BootstrapServers = settings.Value.BootstrapServers,
                GroupId = settings.Value.GroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task[] UnionTasks = new Task[]
            {
                Task.Run(() => OrderCandidateOccuredProcessSuccessConsumer(stoppingToken)),
                Task.Run(() => OrderCandidateOccuredProcessFailedConsumer(stoppingToken)),
                Task.Run(() => ProductSoldConsumer(stoppingToken)),
                Task.Run(() => ProductRemovedConsumer(stoppingToken)),

            };

            return Task.WhenAll(UnionTasks);
        }

        private async Task OrderCandidateOccuredProcessSuccessConsumer(CancellationToken cancellationToken)
        {
            using (var consumerBuilder = new ConsumerBuilder<Ignore, OrderCandidateOccuredProcessSuccess>(_config)
                .SetValueDeserializer(new ProtoDeserializer<OrderCandidateOccuredProcessSuccess>())
                .Build())
            {
                consumerBuilder.Subscribe("OrderCandidateProcessSuccess");
                var cancelToken = new CancellationTokenSource();

                while(!cancelToken.IsCancellationRequested)
                {
                    var consumer = consumerBuilder.Consume(cancelToken.Token);

                    var message = consumer.Message.Value;

                    try
                    {
                        await _service.CloseOrders(message);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"OrderService KafkaConsumerService Exception - {e.Message}");
                        continue;
                    }
                    _logger.LogInformation($"OrderService KafkaConsumerService Orders closed: {message.OrderId}," +
                        $" {message.OrderIdSeller}");
                }
            }

        }

        private async Task OrderCandidateOccuredProcessFailedConsumer(CancellationToken cancellationToken)
        {
            using (var consumerBuilder = new ConsumerBuilder<Ignore, OrderCandidateOccuredProcessFailed>(_config)
                .SetValueDeserializer(new ProtoDeserializer<OrderCandidateOccuredProcessFailed>())
                .Build())
            {
                consumerBuilder.Subscribe("OrderCandidateOccuredProcessFailded");
                var cancelToken = new CancellationTokenSource();

                while(!cancelToken.IsCancellationRequested)
                {
                    var consumer = consumerBuilder.Consume(cancelToken.Token);

                    var message = consumer.Message.Value;

                    _logger.LogError($"OrderMicroservice KafkaConsumerService Exception - Transaction failed. " +
                        $"{message.OrderId}, {message.OrderIdSeller}");
                }
            }
        }

        private async Task ProductSoldConsumer(CancellationToken cancellationToken)
        {
            using (var consumerBuilder = new ConsumerBuilder<Ignore, ProductSoldEvent>(_config)
                .SetValueDeserializer(new ProtoDeserializer<ProductSoldEvent>())
                .Build())
            {
                consumerBuilder.Subscribe("ProductSoldEvent");
                var cancelToken = new CancellationTokenSource();

                while (!cancelToken.IsCancellationRequested)
                {
                    var consumer = consumerBuilder.Consume(cancelToken.Token);

                    var message = consumer.Message.Value;
                    try
                    {
                        await _service.DeleteOrdersSoldProducts(message);
                    }
                    catch
                    {
                        _logger.LogError("OrderService KafkaConsumerService Exception");
                    }
                }
            }
        }

        private async Task ProductRemovedConsumer(CancellationToken cancellationToken)
        {
            using(var consumerBuilder = new ConsumerBuilder<Null, ProductRemovedEvent>(_config)
                .SetValueDeserializer(new ProtoDeserializer<ProductRemovedEvent>())
                .Build())
            {
                consumerBuilder.Subscribe("ProductRemovedEvent");
                var cancelToken = new CancellationTokenSource();

                while(!cancelToken.IsCancellationRequested)
                {
                    var consumer = consumerBuilder.Consume(cancelToken.Token);

                    var message = consumer.Message.Value;
                    try
                    {
                        await _service.DeleteOrdersRemovedProducts(message);
                    }
                    catch(Exception e)
                    {
                        _logger.LogError("OrderService KafkaConsumerService Exception " + e.Message);
                    }
                }
            }
        }
    }
}
