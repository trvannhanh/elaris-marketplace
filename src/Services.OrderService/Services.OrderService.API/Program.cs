using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using BuildingBlocks.Contracts.Events;
using MassTransit;

using Services.OrderService.Services.OrderService.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Services.OrderService.Infrastructure.Persistence;
using Services.OrderService.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Connection
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

// Infra setup
builder.Services.AddInfrastructure(conn);

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Services.OrderService"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit")
        .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("MassTransit")
        .AddPrometheusExporter());

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order API", Version = "v1" });
    c.AddServer(new OpenApiServer { Url = "/order" });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoint
app.MapPost("/orders", async (OrderInput input, OrderDbContext db, IPublishEndpoint publisher) =>
{
    var order = new Services.OrderService.Domain.Entities.Order
    {
        Id = Guid.NewGuid(),
        ProductId = input.ProductId,
        Quantity = input.Quantity,
        TotalPrice = input.TotalPrice,
        CreatedAt = input.CreatedAt
    };

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    await publisher.Publish(new OrderCreatedEvent(order.Id, order.ProductId, order.TotalPrice, order.CreatedAt));

    return Results.Created($"/orders/{order.Id}", order);
});

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.Migrate();
}

app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.Run();
