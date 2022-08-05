using ProductService;
using ProductService.Configs;
using ProductService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

// ���������� � DI ������������ ��� ��������
builder.Services.Configure<ProductStoreDatabaseSettings>(
    builder.Configuration.GetSection("ProductStoreDatabase"));
builder.Services.Configure<KafkaConsumerSettings>(
    builder.Configuration.GetSection("BootstrapServerKafka"));

// ��������� ����������� ProductContext
builder.Services.AddSingleton<ProductContext>();
// ���������� � ProductService
builder.Services.AddSingleton<ProductService.Services.ProductService>();
// ���������� ����������� �������� ������� KafkaConsumerService
builder.Services.AddHostedService<KafkaConsumerService>();

// ����������� Serilog ��� �����
builder.Host.UseSerilog((context, config) => config
                        .WriteTo.Console());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<ProductServiceGrpc>();

app.Run();
