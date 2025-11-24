using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Services.CatalogService.Data;
using Services.CatalogService.Features.Products.GetProducts;
using Services.CatalogService.Features.Products.GetProduct;
using Services.CatalogService.Features.Products.CreateProduct;
using Services.CatalogService.Features.Products.UpdateProduct;
using Services.CatalogService.Features.Products.DeleteProduct;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using MongoDB.Driver;
using Services.CatalogService.Models;
using OpenTelemetry.Logs;
using BuildingBlocks.Infrastucture.Authentication;
using Services.CatalogService.Features.Products.GetAllProducts;
using Services.CatalogService.Features.Products.GetMyProducts;
using Services.CatalogService.Features.Products.GetPendingProducts;
using Services.CatalogService.Features.Products.RejectProduct;
using Services.CatalogService.Features.Products.ApproveProduct;
using Microsoft.Extensions.Options;
using Minio;
using Services.CatalogService.Config;
using Services.CatalogService.Services;

var builder = WebApplication.CreateBuilder(args);

// MongoDB
builder.Services.AddSingleton<MongoContext>();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Services.CatalogService"))
    //traces
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit")
        .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")))
    //metrics
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("MassTransit")
        .AddPrometheusExporter());

//logs
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317"));
});

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Program).Assembly);
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

// ==================== JWT AUTHENTICATION ====================
builder.Services.AddJwtAuthentication(
    builder.Configuration,
    authorityUrl: "http://identityservice:8080",
    audience: "elaris.api"
);

builder.Services.AddAuthorizationPolicies();

builder.Services.Configure<MinIOOptions>(
    builder.Configuration.GetSection("MinIO"));

builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<MinIOOptions>>().Value;
    var client = new MinioClient()
        .WithEndpoint(options.Endpoint)
        .WithCredentials(options.AccessKey, options.SecretKey)
        .WithSSL(options.UseSSL ? true : false)
        .Build();
    return client;
});

builder.Services.AddScoped<IFileStorageService, MinIOService>();

// Serilog
builder.Host.UseSerilog((ctx, lc) =>
{
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .WriteTo.Console();
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
    c.AddServer(new OpenApiServer { Url = "/catalog" });
});

var app = builder.Build();

// Tạo MongoDB indexes lúc startup
using (var scope = app.Services.CreateScope())
{
    
    var ctx = scope.ServiceProvider.GetRequiredService<MongoContext>();
    var indexKeys = Builders<Product>.IndexKeys
        .Text(p => p.Name)
        .Text(p => p.Description);
    await ctx.Products.Raw.Indexes.CreateOneAsync(new CreateIndexModel<Product>(indexKeys));

    var compoundKeys = Builders<Product>.IndexKeys
        .Ascending(p => p.Price)
        .Descending(p => p.CreatedAt);
    await ctx.Products.Raw.Indexes.CreateOneAsync(new CreateIndexModel<Product>(compoundKeys));

    var isDeletedIndex = Builders<Product>.IndexKeys.Ascending(p => p.IsDeleted);
    await ctx.Products.Raw.Indexes.CreateOneAsync(new CreateIndexModel<Product>(isDeletedIndex));

}

// Middleware
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health");

// MAP TẤT CẢ ENDPOINTS
app.MapGetProducts();
app.MapGetProduct();
app.MapGetAllProducts();
app.MapGetMyProducts();
app.MapGetPendingProducts();

app.MapCreateProduct();

app.MapUpdateProduct();
app.MapRejectProduct();
app.MapApproveProduct();

app.MapDeleteProduct();

app.Run();