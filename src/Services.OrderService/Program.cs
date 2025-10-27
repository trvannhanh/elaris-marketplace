using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Services.OrderService.Consumers;
using Services.OrderService.Data;
using Services.OrderService.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// config db
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
           ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

builder.Services.AddDbContext<OrderDbContext>(opt =>
    opt.UseNpgsql(conn, npgsql => npgsql.EnableRetryOnFailure()));

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });

    c.AddServer(new OpenApiServer
    {
        Url = "/order" // 👈 quan trọng
    });

});

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
    x.SetKebabCaseEndpointNameFormatter();

    x.AddConsumer<ProductPriceUpdatedConsumer>();

    x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(10);
        o.DuplicateDetectionWindow = TimeSpan.FromMinutes(1);
        o.UsePostgres();
        //o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ConfigureEndpoints(context);
    });

});


builder.Services.AddLogging();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{

    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.Migrate();

}


OrderEndpoints.MapOrderEndpoints(app);
// Prometheus metrics
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
