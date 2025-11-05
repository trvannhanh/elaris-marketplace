using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.EntityFrameworkCore;
using Services.OrderService.Infrastructure.Persistence;
using Services.OrderService.Infrastructure.Extensions;
using MediatR;
using FluentValidation;
using Services.OrderService.Application.Common.Behaviors;
using Mapster;
using Services.OrderService.Application.Common.Mappings;
using MapsterMapper;
using Services.OrderService.API.Middleware;
using MassTransit;
using Services.OrderService.Infrastructure.Saga;




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

builder.Services.AddControllers();


//MediaR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Services.OrderService.Application.AssemblyReference).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(Services.OrderService.Application.AssemblyReference).Assembly,
    includeInternalTypes: true
);

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));


// Mapster
var config = TypeAdapterConfig.GlobalSettings;
config.Scan(typeof(OrderMappingConfig).Assembly);

builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();

//Saga
builder.Services.AddOrderSaga(builder.Configuration);

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Order API", Version = "v1" });
    c.AddServer(new OpenApiServer { Url = "/order" });
});

// MassTransit
builder.Services.AddMassTransitHostedService(true);

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
app.AddGlobalExceptionHandler(logger);


app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    context.Database.Migrate();
}

app.MapControllers();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.Run();
