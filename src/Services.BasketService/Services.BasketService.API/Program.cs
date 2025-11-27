// Services.BasketService.API/Program.cs
using StackExchange.Redis;
using Services.BasketService.Infrastructure.Repositories;
using Services.BasketService.Application.Interfaces;
using Services.BasketService.Application.Models;
using Services.BasketService.Infrastructure.Services;
using MassTransit;
using FluentValidation;
using BuildingBlocks.Contracts.Events;
using Services.BasketService.API.Extensions;
using Microsoft.OpenApi.Models;
using Services.BasketService.Application.Validators;
using FluentValidation.AspNetCore;
using Polly.Extensions.Http;
using Polly;
using Prometheus;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using BuildingBlocks.Infrastucture.Authentication;

var builder = WebApplication.CreateBuilder(args);

// ==================== REDIS ====================
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("redis:6379"));

builder.Services.AddScoped<IBasketRepository, BasketRepository>();

// ==================== JWT AUTHENTICATION ====================
builder.Services.AddJwtAuthentication(
    builder.Configuration,
    authorityUrl: "http://identityservice:8080",
    audience: "elaris.api"
);

// ==================== AUTHORIZATION POLICIES ====================
builder.Services.AddAuthorizationPolicies();

// ==================== FLUENT VALIDATION ====================
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<BasketItemValidator>();

// ==================== HTTP CLIENT WITH POLLY ====================
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri("http://catalogservice:8080");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddPolicyHandler((services, request) =>
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                var error = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString();
                logger.LogWarning(
                    "[Polly] Retry {RetryAttempt}/{TotalRetries} after {Delay}s | Reason: {Error}",
                    retryAttempt, 3, timespan.TotalSeconds, error);
            });
})
.AddPolicyHandler((services, request) =>
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDuration) =>
            {
                logger.LogError("[Polly] Circuit BREAKER OPENED for {Duration}s", breakDuration.TotalSeconds);
            },
            onReset: () =>
            {
                logger.LogInformation("[Polly] Circuit BREAKER CLOSED");
            });
});

builder.Services.AddEndpointsApiExplorer();

// ==================== SWAGGER ====================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Basket API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: \"Authorization: Bearer {token}\"",
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

    c.AddServer(new OpenApiServer { Url = "/basket" });
});

// ==================== MASSTRANSIT ====================
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

// ==================== OPENTELEMETRY ====================
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("Services.BasketService"))
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

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter(o => o.Endpoint = new Uri("http://otel-collector:4317"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ==================== PROMETHEUS ====================
app.UseHttpMetrics();
app.MapMetrics();

// ==================== BASKET API ENDPOINTS ====================

/// <summary>
/// Get basket - Lấy giỏ hàng của user hiện tại
/// </summary>
app.MapGet("/api/basket", async (
    HttpContext ctx,
    IBasketRepository repo,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();
    var items = await repo.GetBasketAsync(userId, ct);
    var itemsList = items.ToList();

    var subTotal = itemsList.Sum(i => i.Price * i.Quantity);

    return Results.Ok(new BasketDto
    {
        UserId = userId,
        Items = itemsList.Select(i => new BasketItemDto
        {
            ProductId = i.ProductId,
            Name = i.Name,
            Price = i.Price,
            Quantity = i.Quantity,
            ImageUrl = i.ImageUrl
        }).ToList(),
        SubTotal = subTotal,
        Discount = 0, // TODO: Calculate discount from voucher
        Total = subTotal,
        TotalItems = itemsList.Sum(i => i.Quantity),
        LastUpdated = itemsList.Any() ? itemsList.Max(i => i.UpdatedAt) : null
    });
})
.RequireAuthorization("BuyerOrSeller")
.WithName("GetBasket")
.WithSummary("Get user's basket")
.WithTags("Basket")
.WithDescription("""
                - User: Xem danh sách các sản phẩm trong giỏ hàng của mình
                """);

/// <summary>
/// Get basket summary - Lấy tóm tắt giỏ hàng (count + total)
/// </summary>
app.MapGet("/api/basket/summary", async (
    HttpContext ctx,
    IBasketRepository repo,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();
    var count = await repo.GetBasketCountAsync(userId, ct);
    var total = await repo.GetBasketTotalAsync(userId, ct);

    return Results.Ok(new BasketSummaryDto
    {
        TotalItems = count,
        Total = total
    });
})
.RequireAuthorization("BuyerOrSeller")
.WithName("GetBasketSummary")
.WithSummary("Get basket summary (count + total)")
.WithTags("Basket")
.WithDescription("""
                - User: Lấy thông tin tóm tắt từ giỏ hàng của mình gồm tổng số lượng sản phẩm và tổng tiền
                """);

/// <summary>
/// Add item to basket - Thêm sản phẩm vào giỏ
/// </summary>
app.MapPost("/api/basket/items", async (
    HttpContext ctx,
    AddBasketItemRequest request,
    IBasketRepository repo,
    ICatalogServiceClient catalog,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();

    // Validate request
    if (string.IsNullOrEmpty(request.ProductId))
        return Results.BadRequest("ProductId is required");

    if (request.Quantity <= 0)
        return Results.BadRequest("Quantity must be greater than 0");

    // Get product from catalog
    var product = await catalog.GetProductAsync(request.ProductId, ct);
    if (product == null)
    {
        logger.LogWarning("[AddToBasket] Product {ProductId} not found", request.ProductId);
        return Results.NotFound("Product not found");
    }

    // Check existing item
    var existingItem = await repo.GetItemAsync(userId, request.ProductId, ct);

    var item = new BasketItem
    {
        ProductId = request.ProductId,
        Name = product.Name,
        Price = product.Price,
        ImageUrl = product.PreviewImageUrl,
        Quantity = existingItem != null ? existingItem.Quantity + request.Quantity : request.Quantity
    };

    await repo.AddOrUpdateItemAsync(userId, item, ct);

    logger.LogInformation("[AddToBasket] User {UserId} added {Quantity}x {ProductName} to basket",
        userId, request.Quantity, product.Name);

    return Results.Ok(new { Message = "Item added to basket", Item = item });
})
.RequireAuthorization("BuyerOrSeller")
.WithName("AddItemToBasket")
.WithSummary("Add item to basket")
.WithTags("Basket")
.WithDescription("""
                - User: Thực hiện thêm sản phẩm vào giỏ của mình
                """);

/// <summary>
/// Update item quantity - Cập nhật số lượng sản phẩm
/// </summary>
app.MapPut("/api/basket/items/{productId}", async (
    HttpContext ctx,
    string productId,
    UpdateBasketItemRequest request,
    IBasketRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();

    if (request.Quantity <= 0)
        return Results.BadRequest("Quantity must be greater than 0");

    var existingItem = await repo.GetItemAsync(userId, productId, ct);
    if (existingItem == null)
        return Results.NotFound("Item not found in basket");

    existingItem.Quantity = request.Quantity;
    existingItem.UpdatedAt = DateTime.UtcNow;

    await repo.AddOrUpdateItemAsync(userId, existingItem, ct);

    logger.LogInformation("[UpdateBasketItem] User {UserId} updated {ProductId} quantity to {Quantity}",
        userId, productId, request.Quantity);

    return Results.Ok(new { Message = "Item quantity updated", Item = existingItem });
})
.RequireAuthorization("BuyerOrSeller")
.WithName("UpdateItemQuantity")
.WithSummary("Update item quantity in basket")
.WithTags("Basket")
.WithDescription("""
                - User: Thực hiện cập nhật số lượng cho sản phẩm trong giỏ hàng của mình
                """);

/// <summary>
/// Remove item from basket - Xóa sản phẩm khỏi giỏ
/// </summary>
app.MapDelete("/api/basket/items/{productId}", async (
    HttpContext ctx,
    string productId,
    IBasketRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();
    var success = await repo.RemoveItemAsync(userId, productId, ct);

    if (!success)
        return Results.NotFound("Item not found in basket");

    logger.LogInformation("[RemoveFromBasket] User {UserId} removed {ProductId} from basket",
        userId, productId);

    return Results.NoContent();
})
.RequireAuthorization("BuyerOrSeller")
.WithName("RemoveItemFromBasket")
.WithSummary("Remove item from basket")
.WithTags("Basket")
.WithDescription("""
                - User: Thực hiện xóa 1 sản phẩm trong giỏ hàng của mình truyền vào Id sản phẩm
                """);

/// <summary>
/// Clear basket - Xóa toàn bộ giỏ hàng
/// </summary>
app.MapDelete("/api/basket", async (
    HttpContext ctx,
    IBasketRepository repo,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();
    await repo.ClearBasketAsync(userId, ct);

    logger.LogInformation("[ClearBasket] User {UserId} cleared their basket", userId);

    return Results.NoContent();
})
.RequireAuthorization("BuyerOrSeller")
.WithName("ClearBasket")
.WithSummary("Clear all items from basket")
.WithTags("Basket")
.WithDescription("""
                - User: Thực hiện xóa tất cả sản phẩm trong giỏ hàng của mình 
                """);

/// <summary>
/// Checkout - Thanh toán giỏ hàng
/// </summary>
app.MapPost("/api/basket/checkout", async (
    HttpContext ctx,
    CheckoutRequest request,
    IBasketRepository repo,
    IPublishEndpoint publisher,
    ILogger<Program> logger,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();
    var userName = ctx.GetName();

    logger.LogInformation("[Checkout] User {UserName} ({UserId}) requested checkout", userName, userId);

    var items = (await repo.GetBasketAsync(userId, ct)).ToList();

    if (!items.Any())
    {
        logger.LogWarning("[Checkout] Basket is empty for user {UserId}", userId);
        return Results.BadRequest(new CheckoutResultDto
        {
            Success = false,
            Message = "Basket is empty"
        });
    }

    var totalAmount = items.Sum(i => i.Price * i.Quantity);

    logger.LogInformation("[Checkout] Basket has {Count} items, total: {Total}", items.Count, totalAmount);

    // Publish event
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
        logger.LogInformation("[Checkout] ✅ BasketCheckedOutEvent published for user {UserId}", userId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Checkout] ❌ Failed to publish event for user {UserId}", userId);
        return Results.Problem("Failed to process checkout");
    }

    // Clear basket
    await repo.ClearBasketAsync(userId, ct);
    logger.LogInformation("[Checkout] Basket cleared for user {UserId}", userId);

    return Results.Accepted("/api/basket/checkout", new CheckoutResultDto
    {
        Success = true,
        Message = "Checkout successful. Your order is being processed.",
        TotalAmount = totalAmount
    });
})
.RequireAuthorization("BuyerOrSeller")
.WithName("CheckoutBasket")
.WithSummary("Checkout basket and create order")
.WithTags("Basket")
.WithDescription("""
                - User: Thực hiện checkout giỏ hàng của mình để bắt đầu luồng xử lý tạo order và thanh toán, xử lý kho
                """);

app.Run();