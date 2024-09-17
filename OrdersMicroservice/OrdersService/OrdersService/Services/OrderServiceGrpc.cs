using AutoMapper;
using Confluent.Kafka;
using Grpc.Core;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using OrdersService.Data.Abstractions;
using OrdersService.Data.Repository;
using OrdersService.Models;
using OrdersService.Protos;
using OrdersService.Services.KafkaSettingsFolder;

namespace OrdersService.Services
{
    // Добавить обработку ошибок
    public class OrderServiceGrpc : OrderProcessing.OrderProcessingBase
    {
        private readonly ILogger<OrderServiceGrpc> _logger;
        // Убрать репозитории
        private readonly IMongoRepository<ActiveSellOrder> _repositorySellOrder;
        private readonly IMongoRepository<ActiveBuyOrder> _repositoryBuyOrder;
        private readonly IMongoRepository<CompletedOrder> _repositoryCompleted;
        private readonly IMongoRepository<Order> _orderRepository;
        private readonly IMongoRepository<BestProductPrice> _productPriceRepository;
        private readonly IMapper _mapper;
        private readonly IOptions<KafkaSettings> _config;
        private readonly OrderOperationService _service;

        public OrderServiceGrpc(ILogger<OrderServiceGrpc> logger, 
                                      IMongoRepository<ActiveSellOrder> repositorySellOrder,
                                      IMongoRepository<ActiveBuyOrder> repositoryBuyOrder,
                                      IMongoRepository<CompletedOrder> repositoryCompleted,
                                      IMongoRepository<Order> orderRepository,
                                      IMongoRepository<BestProductPrice> productPriceRepository,
                                      IOptions<KafkaSettings> kafkaSettings,
                                      OrderOperationService service,
                                      IMapper mapper)
        {
            _logger = logger;
            _repositorySellOrder = repositorySellOrder;
            _repositoryBuyOrder = repositoryBuyOrder;
            _repositoryCompleted = repositoryCompleted;
            _orderRepository = orderRepository;
            _productPriceRepository = productPriceRepository;
            _service = service;
            _mapper = mapper;

            // Переделать
            // Переместить создание индексов для коллекций 
            //_repositorySellOrder.CreateIndexAsync();
            //_repositoryBuyOrder.CreateIndexAsync();
            //_repositoryCompleted.CreateIndexAsync();
            //_orderRepository.CreateIndexAsync();
            //_productPriceRepository.CreateIndexAsync();

            _config = kafkaSettings;
        }
        
        public override async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, 
            ServerCallContext context)
        {
            var orderData = _mapper.Map<Models.Messages.DataCreateOrder>(request.Data);
            var type = _mapper.Map<Models.Enums.OrderType>(request.Data.Type);

            var userId = orderData.UserId;
            var orderId = ObjectId.GenerateNewId();
            var price = Protos.CustomTypes.DecimalValue.ToDecimal(orderData.Price);

            var mainOrder = new Models.Order();
            mainOrder.Id = orderId;
            mainOrder.UserId = userId;
            mainOrder.OrderType = type;
            mainOrder.ProductId = orderData.ProductId;
            mainOrder.ProductName = orderData.ProductName;
            mainOrder.Quantity = orderData.Quantity;
            mainOrder.Price = price;

            await _orderRepository.InsertOneAsync(mainOrder);            

            if (type == Models.Enums.OrderType.SELL_ORDER)
                CreateSellOrder(orderData, orderId);
            else
                CreateBuyOrder(orderData, orderId);

            return new CreateOrderResponse
            {
                Success = new SuccessResponse()
            };
        }

        // Переделать 
        private async void CreateSellOrder(Models.Messages.DataCreateOrder orderData, ObjectId orderId)
        {
            await Task.Run(async () =>
            {
                var sellOrder = new ActiveSellOrder();

                sellOrder.Id = orderId;
                sellOrder.InTransaction = false;
                sellOrder.UserId = orderData.UserId;

                await _repositorySellOrder.InsertOneAsync(sellOrder);

                var price = Protos.CustomTypes.DecimalValue.FromDecimal(orderData.Price);

                var messagePrice = new DecimalValue
                {
                    Nanos = price.Nanos,
                    Units = price.Units
                };

                var producer = new KafkaProducerService(_config);
                SellOrderCreated message = new SellOrderCreated()
                {
                    Id = orderData.ProductId,
                    Name = orderData.ProductName,
                    Quantity = orderData.Quantity,
                    Price = messagePrice,
                    UserId = orderData.UserId
                };

                await producer.ProduceMessageAsync(message, "SellOrderCreated");

                var priceDecimal = Protos.CustomTypes.DecimalValue.ToDecimal(orderData.Price);

                var data = new DataCreateBestPrice(orderData.ProductId,
                                   orderData.ProductName,
                                   orderId.ToString(),
                                   priceDecimal,
                                   Models.Enums.PriceType.SellPrice);

                await _service.CreateBestPrice(data);

                await _service.FindSimilarOrders(orderData, orderId);
            });
        }

        // Переделать 
        private async void CreateBuyOrder(Models.Messages.DataCreateOrder orderData, ObjectId orderId)
        {
            await Task.Run(async () =>
            {
                var buyOrder = new ActiveBuyOrder();

                buyOrder.Id = orderId;
                buyOrder.InTransaction = false;
                buyOrder.UserId = orderData.UserId;

                await _repositoryBuyOrder.InsertOneAsync(buyOrder);

                var price = Protos.CustomTypes.DecimalValue.FromDecimal(orderData.Price);

                var messagePrice = new DecimalValue
                {
                    Nanos = price.Nanos,
                    Units = price.Units
                };

                var producer = new KafkaProducerService(_config);
                BuyOrderCreated message = new BuyOrderCreated()
                {
                    Id = orderData.ProductId,
                    Name = orderData.ProductName,
                    Quantity = orderData.Quantity,
                    Price = messagePrice,
                    UserId = orderData.UserId
                };

                await producer.ProduceMessageAsync(message, "BuyOrderCreated");

                var priceDecimal = Protos.CustomTypes.DecimalValue.ToDecimal(orderData.Price);

                var data = new DataCreateBestPrice(orderData.ProductId,
                                                   orderData.ProductName,
                                                   orderId.ToString(),
                                                   priceDecimal,
                                                   Models.Enums.PriceType.BuyPrice);

                await _service.CreateBestPrice(data);

                await _service.FindSimilarOrders(orderData, orderId);
            });
        }

        // 2 метода выше вынести в один
    }
}
