using Microsoft.Extensions.Options;
using OrdersService.Data.Repository;
using OrdersService.Data.Settings;
using OrdersService.KafkaServices;
using OrdersService.Services;
using OrdersService.Services.KafkaSettingsFolder;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;

builder.Services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));

builder.Services.Configure<KafkaSettings>(configuration.GetSection("KafkaSettings"));

builder.Services.AddSingleton<IMongoDbSettings>(serviceProvider =>
    serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value);

builder.Services.AddTransient(typeof(IMongoRepository<>), typeof(MongoRepository<>));

builder.Services.AddTransient(typeof(OrderOperationService));
builder.Services.AddTransient(typeof(KafkaProducerService));
builder.Services.AddHostedService<KafkaConsumerService>();

builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddGrpc();

var app = builder.Build();

var topicService = new AdminTopicBuilderService(app);
topicService.TopicBuildAsync();

app.MapGrpcService<GreeterService>();
app.MapGrpcService<OrderServiceGrpc>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
