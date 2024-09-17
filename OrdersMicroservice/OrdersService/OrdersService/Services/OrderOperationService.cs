using AutoMapper;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using OrdersService.Data.Repository;
using OrdersService.Models;
using OrdersService.Protos;
using OrdersService.Services.KafkaSettingsFolder;

namespace OrdersService.Services
{
    // Добавить обработку ошибок
    public class OrderOperationService
    {
        // Переделать
        // Убрать репозитории
        private readonly ILogger<OrderServiceGrpc> _logger;
        private readonly IMongoRepository<ActiveSellOrder> _repositorySellOrder;
        private readonly IMongoRepository<ActiveBuyOrder> _repositoryBuyOrder;
        private readonly IMongoRepository<CompletedOrder> _repositoryCompleted;
        private readonly IMongoRepository<Order> _orderRepository;
        private readonly IMongoRepository<BestProductPrice> _priceRepository;
        private readonly IOptions<KafkaSettings> _config;
        private readonly IMapper _mapper;

        public OrderOperationService(ILogger<OrderServiceGrpc> logger,
                                      IMongoRepository<ActiveSellOrder> repositorySellOrder,
                                      IMongoRepository<ActiveBuyOrder> repositoryBuyOrder,
                                      IMongoRepository<CompletedOrder> repositoryCompleted,
                                      IMongoRepository<Order> orderRepository,
                                      IMongoRepository<BestProductPrice> priceRepository,
                                      IOptions<KafkaSettings> settings,
                                      IMapper mapper)
        {
            _logger = logger;
            _repositorySellOrder = repositorySellOrder;
            _repositoryBuyOrder = repositoryBuyOrder;
            _repositoryCompleted = repositoryCompleted;
            _orderRepository = orderRepository; 
            _priceRepository = priceRepository;
            _mapper = mapper;

            _config = settings;
        } 

        public async Task CloseOrders(OrderCandidateOccuredProcessSuccess message)
        {
            var result = _mapper.Map<Models.Messages.OrderCandidateOccuredProcessSuccess>(message);

            var firstOrderId = result.OrderId;
            var firstUserId = _orderRepository.FindById(firstOrderId).UserId;

            var secondOrderId = result.OrderIdSeller;
            var secondUserId = _orderRepository.FindById(secondOrderId).UserId;

            var price = _orderRepository.FindById(firstOrderId).Price;
            var quantity = _orderRepository.FindById(firstOrderId).Quantity;
            var productName = _orderRepository.FindById(firstOrderId).ProductName;

            // Убрать дублирование
            await _repositoryBuyOrder.DeleteByIdAsync(firstOrderId.ToString());
            await _repositorySellOrder.DeleteByIdAsync(secondOrderId.ToString());
            await _orderRepository.DeleteByIdAsync(firstOrderId.ToString());
            await _orderRepository.DeleteByIdAsync(secondOrderId.ToString());

            var firstCompletedOrder = await CreateCompletedOrder(firstUserId, firstOrderId, secondUserId, 
                secondOrderId, price, quantity, productName);
            var secondCompletedOrder = await CreateCompletedOrder(secondUserId, secondOrderId, firstUserId, 
                firstOrderId, price, quantity, productName);

            await _repositoryCompleted.InsertManyAsync(new List<CompletedOrder> { 
                firstCompletedOrder, secondCompletedOrder 
            });
        }

        private async Task<CompletedOrder> CreateCompletedOrder(string firstUserId, 
            string firstOrderId, string secondUserId, string secondOrderId, decimal price, int quantity, string productName)
        {
            var completedOrder = new CompletedOrder();

            completedOrder.Id = new ObjectId(firstUserId);
            completedOrder.OrderId = firstOrderId;
            completedOrder.UserIdSecond = secondUserId;
            completedOrder.OrderIdSecond = secondOrderId;
            completedOrder.Price = price;

            return completedOrder;
        }

        // Убрать дублирование
        public async Task DeleteOrdersSoldProducts(ProductSoldEvent message)
        {
            var userId = message.UserId;
            var productId = message.ProductId;
            var quantity = message.Quantity;

            IEnumerable<Order> orders = _orderRepository.FilterBy(filter =>
                    filter.UserId == userId &&
                    filter.OrderType == Models.Enums.OrderType.SELL_ORDER &&
                    filter.ProductId == productId &&
                    filter.Quantity > quantity);

            foreach (var order in orders)
            {
                await _repositorySellOrder.DeleteByIdAsync(order.Id.ToString());
            }

            _orderRepository.DeleteMany(filter =>
                    filter.UserId == userId &&
                    filter.OrderType == Models.Enums.OrderType.SELL_ORDER &&
                    filter.ProductId == productId &&
                    filter.Quantity > quantity);
        }

        // Убрать дублирование 
        public async Task DeleteOrdersRemovedProducts(ProductRemovedEvent message)
        {
            var userId = message.UserId;
            var productId = message.ProductId;

            IEnumerable<Order> ordersId = _orderRepository.FilterBy(filter =>
                filter.UserId == userId &&
                filter.ProductId == productId
                );

            foreach (var order in ordersId)
            {
                await _repositorySellOrder.DeleteByIdAsync(order.Id.ToString());
            }

            _orderRepository.DeleteMany(filter => 
                filter.UserId == userId &&
                filter.ProductId == productId &&
                filter.OrderType == Models.Enums.OrderType.SELL_ORDER);
        }

        // Переделать, переместить в OrderOperationService. Создать событие и подписаться
        public async Task CreateBestPrice(DataCreateBestPrice data)
        {
            var documentProductPrice = await _priceRepository.FindOneAsync(filter =>
                filter.Id == new ObjectId(data.ProductId));

            // Вывести в метод
            if (documentProductPrice is null)
            {
                var bestProductPrice = new BestProductPrice();

                bestProductPrice.Id = new ObjectId(data.ProductId);
                bestProductPrice.OrderId = data.OrderId;

                // Вывести в метод
                if (data.PriceType == Models.Enums.PriceType.SellPrice)
                    bestProductPrice.BestSellPrice = data.Price;
                else
                    bestProductPrice.BestBuyPrice = data.Price;

                await _priceRepository.InsertOneAsync(bestProductPrice);
            }
            else
            {
                // Вывести в метод
                var replaceDocument = new BestProductPrice();
                replaceDocument.Id = new ObjectId(data.ProductId);
                replaceDocument.OrderId = data.OrderId;

                // Вывести в метод
                if (data.PriceType == Models.Enums.PriceType.SellPrice)
                {
                    replaceDocument.BestBuyPrice = _priceRepository.FindById(data.ProductId.ToString()).BestBuyPrice;
                    replaceDocument.BestSellPrice = data.Price;
                }
                else
                {
                    replaceDocument.BestSellPrice = _priceRepository.FindById(data.ProductId.ToString()).BestSellPrice;
                    replaceDocument.BestBuyPrice = data.Price;
                }

                await _priceRepository.ReplaceOneAsync(replaceDocument);

                var producer = new KafkaProducerService(_config);

                var price = _mapper.Map<Protos.DecimalValue>(Protos.CustomTypes.DecimalValue.FromDecimal(data.Price));
                
                // Исправить
                var message = new Models.Messages.ProductPriceChanged();
                message.ProductId = data.ProductId;
                message.ProductName = data.ProductName;
                message.PriceType = data.PriceType;
                message.Price = price;

                var priceChanged = _mapper.Map<Protos.ProductPriceChanged>(message);

                await producer.ProduceMessageAsync(priceChanged, "ProductPriceChanged");
            }
        }

        public async Task FindSimilarOrders(Models.Messages.DataCreateOrder data, ObjectId orderId)
        {
            // Оставить только OrderType и active price
            var activeOrderId = orderId;
            var activeOrderType = data.OrderType;
            var activeProductId = data.ProductId;
            var activeProductName = data.ProductName;
            var activeQuantity = data.Quantity;
            var activePrice = Protos.CustomTypes.DecimalValue.ToDecimal(data.Price);

            // Исправить это
            var sellOrder = new Order();
            var buyOrder = new Order();
            
            // Разбить на отдельные выражения
            // читать невозможно
            // Избавиться от If 
            if(activeOrderType == Models.Enums.OrderType.SELL_ORDER)
            {
                sellOrder = await CreateOrder(data, orderId);
                buyOrder = _orderRepository.FilterBy(filter =>
                                                     filter.OrderType == Models.Enums.OrderType.BUY_ORDER &&
                                                     filter.ProductId == activeProductId &&
                                                     filter.Price <= activePrice &&
                                                     filter.Quantity == activeQuantity).FirstOrDefault();
            }
            else
            {
                buyOrder = await CreateOrder(data, orderId);
                sellOrder = _orderRepository.FilterBy(filter =>
                                                      filter.OrderType == Models.Enums.OrderType.SELL_ORDER &&
                                                      filter.ProductId == activeProductId &&
                                                      filter.Price == activePrice &&
                                                      filter.Quantity == activeQuantity).FirstOrDefault();
            }


            if (buyOrder is null ||
                sellOrder is null)
            {
                _logger.LogInformation($"для заявки {activeOrderId} не была найдена подходящая заявка." +
                    $" Она помещена в БД");
                return;
            }

            var priceBuy = Protos.CustomTypes.DecimalValue.FromDecimal(buyOrder.Price);

            var messagePriceBuy = new DecimalValue
            {
                Nanos = priceBuy.Nanos,
                Units = priceBuy.Units
            };

            // Продюсер отправляет в топик
            var producer = new KafkaProducerService(_config);
            var message = new OrderCandidateOccuredProcessEvent()
            {
                OrderId = buyOrder.Id.ToString(),
                OrderIdSeller = sellOrder.Id.ToString(),
                Quantity = buyOrder.Quantity,
                ProductId = buyOrder.ProductId,
                UserIdBuyer = buyOrder.UserId,
                UserIdSeller = sellOrder.UserId,
                Price = messagePriceBuy,
            };

            await producer.ProduceMessageAsync(message, "OrderCandidateOccuredProcessEvent");
        }

        private async Task<Order> CreateOrder(Models.Messages.DataCreateOrder data, ObjectId id)
        {
            // Избавиться от этого
            await Task.Delay(0);

            Order order = new Order();

            var price = Protos.CustomTypes.DecimalValue.ToDecimal(data.Price);

            order.Id = id;
            order.UserId = data.UserId;
            order.OrderType = data.OrderType;
            order.ProductId = data.ProductId;
            order.ProductName = data.ProductName;
            order.Quantity = data.Quantity;
            order.Price = price;

            return order;
        }
    }
}
