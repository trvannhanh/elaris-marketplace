using StackExchange.Redis;
using Services.BasketService.Infrastructure.Repositories;
using Services.BasketService.Application.Interfaces;
using Services.BasketService.Application.Models;
using Services.BasketService.Infrastructure.Services;
using MassTransit;
using BuildingBlocks.Contracts.Events;
using Services.BasketService.API.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("redis:6379"));

builder.Services.AddScoped<IBasketRepository, BasketRepository>();


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

// ✅ Catalog Service HTTP Client
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri("http://catalogservice:8080");
    // tránh bị time-out
    client.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Basket API", Version = "v1" });

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
        Url = "/basket" // 👈 quan trọng
    });

});

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// API
app.MapGet("/api/basket", async (
    HttpContext ctx,
    IBasketRepository repo,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();

    var items = await repo.GetBasketAsync(userId, ct);
    return Results.Ok(new BasketDto
    {
        UserId = userId,
        Items = items?.ToList() ?? new()
    });
}).RequireAuthorization();


app.MapPost("/api/basket", async (
    HttpContext ctx,
    BasketItem item,
    IBasketRepository repo,
    ICatalogServiceClient catalog,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();

    var product = await catalog.GetProductAsync(item.ProductId, ct);
    if (product == null)
        return Results.BadRequest("Product invalid");

    item.Name = product.Name;
    item.Price = product.Price;

    await repo.AddOrUpdateItemAsync(userId, item, ct);
    return Results.Ok();
}).RequireAuthorization();

app.MapPost("/api/basket/checkout", async (
    HttpContext ctx,
    IBasketRepository repo,
    CancellationToken ct,
    IPublishEndpoint publisher) =>
{
    var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
    var userId = ctx.GetUserId();

    logger.LogInformation("Checkout requested for user: {UserId}", userId);

    var items = await repo.GetBasketAsync(userId, ct);

    if (items == null || !items.Any())
    {
        logger.LogWarning("Basket is empty for user: {UserId}", userId);
        return Results.BadRequest("Basket empty!");
    }

    logger.LogInformation("Basket has {Count} items. Publishing event...", items.Count());

    var eventToPublish = new BasketCheckedOutEvent(
        userId,
        items.Select(i => new BasketItemEvent(
            i.ProductId,
            i.Name,
            i.Price,
            i.Quantity
        )).ToList()
    );

    try
    {
        await publisher.Publish(eventToPublish, ct);
        logger.LogInformation("BasketCheckedOutEvent published successfully for user: {UserId}", userId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to publish BasketCheckedOutEvent for user: {UserId}", userId);
        // Không throw → vẫn clear basket
    }

    await repo.ClearBasketAsync(userId, ct);
    logger.LogInformation("Basket cleared for user: {UserId}", userId);

    return Results.Accepted();
}).RequireAuthorization();

app.MapDelete("/api/basket/{productId}", async (
    HttpContext ctx,
    string productId,
    IBasketRepository repo,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();
    var success = await repo.RemoveItemAsync(userId, productId, ct);

    return success ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

app.Run();
