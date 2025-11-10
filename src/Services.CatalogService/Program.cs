using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Services.CatalogService.Data;
using Services.CatalogService.Features.Products.GetProducts;
using Services.CatalogService.Features.Products.GetProduct;
using Services.CatalogService.Features.Products.CreateProduct;
using Services.CatalogService.Features.Products.UpdateProduct;
using Services.CatalogService.Features.Products.UpdatePrice;
using Services.CatalogService.Features.Products.DeleteProduct;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using Serilog;
using MongoDB.Driver;
using Services.CatalogService.Models;

var builder = WebApplication.CreateBuilder(args);

// MongoDB
builder.Services.AddSingleton<MongoContext>();

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var auth = builder.Configuration.GetSection("Authentication");
        options.Authority = auth["Authority"];
        options.Audience = auth["Audience"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            NameClaimType = "name"
        };
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("CatalogService", "1.0.0"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317")))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4318")));

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
    await ctx.Products.Indexes.CreateOneAsync(new CreateIndexModel<Product>(indexKeys));

    var compoundKeys = Builders<Product>.IndexKeys
        .Ascending(p => p.Price)
        .Descending(p => p.CreatedAt);
    await ctx.Products.Indexes.CreateOneAsync(new CreateIndexModel<Product>(compoundKeys));
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
app.MapCreateProduct();
app.MapUpdateProduct();
app.MapUpdatePrice();
app.MapDeleteProduct();

app.Run();