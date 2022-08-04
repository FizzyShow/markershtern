using AuthMicroservice;
using AuthMicroservice.Configs;
using AuthMicroservice.Services;

var builder = WebApplication.CreateBuilder(args);

// ����������� � DI Grpc
builder.Services.AddGrpc();

// ����������� � DI ������������, � ������� �������� MongoDBSettings
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDBSettings"));

// ����������� � DI UsersContext
builder.Services.AddSingleton<UsersContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<AuthService>();

app.Run();
