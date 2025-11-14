

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Services.PaymentService.API.Grpc;
using Services.PaymentService.Application.Payments.Commands;
using Services.PaymentService.Infrastructure.Extensions;
using Services.PaymentService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
           ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION"); // fallback

builder.Services.AddInfrastructure(conn);

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreatePaymentCommand).Assembly));

// Swagger + controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.WebHost.ConfigureKestrel(options =>
{
    // gRPC endpoint nội bộ (HTTP/2)
    options.ListenAnyIP(8081, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);

    // REST + Swagger endpoint (HTTP/1.1)
    options.ListenAnyIP(8080, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
});


var app = builder.Build();

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();
}

app.MapGrpcService<PaymentGrpcService>();

app.MapGet("/", () => "PaymentGrpcService is running...");

app.MapControllers();
app.Run();
