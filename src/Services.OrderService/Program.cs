using MassTransit;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Services.OrderService.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService("Services.OrderService", serviceVersion: "1.0.0"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit") // 👈 Quan trọng
        .AddOtlpExporter(opt =>
        {
            opt.Endpoint = new Uri("http://otel-collector:4317");
            opt.Protocol = OtlpExportProtocol.Grpc;
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("MassTransit")
        .AddPrometheusExporter());

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
// Prometheus metrics
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapHealthChecks("/health");
app.Run();
