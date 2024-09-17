// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Options;
using OrdersService.Services;
using OrdersService.Services.KafkaSettings;
using TestProducers.Protos;

Console.WriteLine("Hello, World!");

KafkaSettings _config = new KafkaSettings()
{
    BootstrapServers = "localhost:9092",
    GroupId = "test_group",
    Topicks = new string[]
    {
        "BuyOrderCreated",
        "SellOrderCreated",
        "ProductPriceChanged",
        "OrderCandidateOccuredEvent",
        "OrderCandidateOccuredProcessFailded",
        "OrderCandidateProcessSuccess",
        "ProductSoldEvent",
        "ProductRemovedEvent"
    }
};

var kafka = new KafkaProducerService(_config);

Console.Write("q - отправить евент удаления товара \r\n" +
              "w - отправить евент продажи товара \r\n" + 
              "e - отправить евент о закрытии 2-ух заявок.");

while (true)
{
    var s = Console.ReadLine();

    if (s == "q")
        SendProductRemoved();
    else if (s == "w")
        SendProductSold();
    else if (s == "e")
        CloseOrders();

}


async Task SendProductRemoved()
{
    var message = new ProductRemovedEvent()
    {
        ProductId = "901f191e110c19729de860ea",
        UserId = "931f1f7732245cd799439011"
    };

    await kafka.ProduceMessageAsync(message, "ProductRemovedEvent");
}

async Task SendProductSold()
{
    var message = new ProductSoldEvent()
    {
        ProductId = "101f222e110c19729de860ea",
        Quantity = 2000,
        UserId = "131f1f7732245cd799439011"
    };

    await kafka.ProduceMessageAsync(message, "ProductSoldEvent");
}

async Task CloseOrders()
{
    var message = new OrderCandidateOccuredProcessSuccess()
    {
        OrderId = "6307ea552d3ef19fa89971f7",
        OrderIdSeller = "6307ea5c2d3ef19fa89971f8"
    };

    await kafka.ProduceMessageAsync(message, "OrderCandidateProcessSuccess");
}