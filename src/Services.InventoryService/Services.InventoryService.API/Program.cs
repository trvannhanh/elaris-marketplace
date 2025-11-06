
using Microsoft.OpenApi.Models;
using Services.InventoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Services.InventoryService.Infrastructure.Extensions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Mapster;
using MapsterMapper;
using Services.InventoryService.Application.Common.Mappings;
using Services.InventoryService.Application.Interfaces;
using Services.InventoryService.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Load connection
var conn = builder.Configuration.GetConnectionString("DefaultConnection")
          ?? Environment.GetEnvironmentVariable("DEFAULT_CONNECTION");

// Infra setup
builder.Services.AddInfrastructure(conn);

// Add Controllers
builder.Services.AddControllers();

// Add MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Services.InventoryService.Application.AssemblyReference).Assembly));

// Add FluentValidation + Behavior Pipeline
//builder.Services.AddValidatorsFromAssembly(
//    typeof(Services.InventoryService.Application.AssemblyReference).Assembly,
//    includeInternalTypes: true);

//builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Mapster
var config = TypeAdapterConfig.GlobalSettings;
config.Scan(typeof(InventoryMappingConfig).Assembly);
builder.Services.AddSingleton(config);
builder.Services.AddScoped<IMapper, ServiceMapper>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Services.InventoryService"))
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
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Inventory API",
        Version = "v1"
    });

    // nếu deploy dưới subpath như /inventory
    c.AddServer(new OpenApiServer { Url = "/inventory" });
});

// MassTransit wait until started
builder.Services.AddOptions<MassTransit.MassTransitHostOptions>()
    .Configure(options => { options.WaitUntilStarted = true; });

builder.Services.AddHttpClient<IInventoryRepository, InventoryRepository>();


var app = builder.Build();

// Migrate DB tự động khi start container
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    context.Database.Migrate();
}

//var logger = app.Services.GetRequiredService<ILogger<Program>>();
//app.AddGlobalExceptionHandler(logger);

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();
