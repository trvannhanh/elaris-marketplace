using MassTransit;
using Services.OrderService.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductPriceUpdatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("product-price-updated-queue", e =>
        {
            e.ConfigureConsumer<ProductPriceUpdatedConsumer>(context);
        });
    });
});

builder.Services.AddLogging();
var app = builder.Build();
app.MapGet("/", () => "OrderService running");
app.Run();
