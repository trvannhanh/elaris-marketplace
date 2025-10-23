using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Models;
using System.Text;
using BuildingBlocks.Contracts.Events;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<MongoContext>();

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "elaris.identity",
            ValidAudience = "elaris.clients",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("supersecretkey_please_change_this_in_prod")),

            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog API", Version = "v1" });

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
        Url = "/catalog" // 👈 quan trọng
    });

});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

// Resource: Metadata cho service (hiển thị trong Jaeger/Tempo)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: builder.Environment.ApplicationName, serviceVersion: "1.0.0"))  // Thay "catalogservice" hoặc "orderservice"
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()  // Trace HTTP requests
        .AddHttpClientInstrumentation()  // Trace outgoing HTTP
        //.AddEntityFrameworkCoreInstrumentation()  // Trace DB (EF Core)
        .AddSource("MassTransit")  // Built-in MassTransit trace
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4317");  // OTLP/gRPC cho Collector (sẽ setup sau)
            options.Protocol = OtlpExportProtocol.Grpc;  // Hoặc Http2
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("MassTransit")  // MassTransit metrics (queue length, message count)
        .AddPrometheusExporter()  // Export sang Prometheus scrape endpoint /metrics
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://otel-collector:4318");
        }));


builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("rabbitmq", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});

var app = builder.Build();

// Endpoint cho Prometheus scrape metrics
app.UseOpenTelemetryPrometheusScrapingEndpoint();  // /metrics

app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// CRUD
app.MapGet("/api/products", async (MongoContext db) =>
{
    var products = await db.Products.Find(x => !x.IsDeleted).ToListAsync();
    return Results.Ok(products);
});

app.MapPost("/api/products", async (MongoContext db, Product p) =>
{
    await db.Products.InsertOneAsync(p);
    return Results.Created($"/api/products/{p.Id}", p);
}).RequireAuthorization("AdminOnly");

app.MapPut("/api/products/{id}", async (MongoContext db, string id, Product updated) =>
{
    var result = await db.Products.ReplaceOneAsync(x => x.Id == id, updated);
    return result.ModifiedCount > 0 ? Results.Ok(updated) : Results.NotFound();
}).RequireAuthorization("AdminOnly");

app.MapPatch("/api/products/{id}/price", async (string id, decimal newPrice, MongoContext db, IPublishEndpoint publisher) =>
{
    var product = await db.Products.Find(p => p.Id == id && !p.IsDeleted).FirstOrDefaultAsync();
    if (product == null) return Results.NotFound();

    var old = product.Price;
    var update = Builders<Product>.Update.Set(p => p.Price, newPrice);
    var res = await db.Products.UpdateOneAsync(p => p.Id == id, update);

    if (res.ModifiedCount > 0)
    {
        // publish event
        var ev = new ProductPriceUpdatedEvent(id, old, newPrice, DateTime.UtcNow);
        await publisher.Publish(ev);
        return Results.Ok(new { productId = id, oldPrice = old, newPrice });
    }

    return Results.BadRequest();
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/products/{id}", async (MongoContext db, string id) =>
{
    var update = Builders<Product>.Update.Set(p => p.IsDeleted, true);
    await db.Products.UpdateOneAsync(x => x.Id == id, update);
    return Results.NoContent();
}).RequireAuthorization("AdminOnly");



app.Run();
