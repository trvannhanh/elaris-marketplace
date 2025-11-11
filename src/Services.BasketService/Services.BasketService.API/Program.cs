using StackExchange.Redis;
using Services.BasketService.Infrastructure.Repositories;
using Services.BasketService.Application.Interfaces;
using Services.BasketService.Application.Models;
using Services.BasketService.Infrastructure.Services;
using MassTransit;
using FluentValidation;
using BuildingBlocks.Contracts.Events;
using Services.BasketService.API.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using Services.BasketService.Application.Validators;
using FluentValidation.AspNetCore;
using Polly.Extensions.Http;
using Polly;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);


// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("redis:6379"));

builder.Services.AddScoped<IBasketRepository, BasketRepository>();


// Duende IdentityServer Authorize
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
builder.Services.AddAuthorization();

//Fluent Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<BasketItemValidator>();

// Catalog Service HTTP Client + Polly (dùng ILogger)
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri("http://catalogservice:8080");
    client.Timeout = TimeSpan.FromSeconds(5);
})
// Policy 1: Retry với backoff tăng dần
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
                    "[Polly] Retry {RetryAttempt}/{TotalRetries} for {Method} {Url} after {Delay}s | Reason: {Error}",
                    retryAttempt, 3, request.Method, request.RequestUri, timespan.TotalSeconds, error);
            });
})
// Policy 2: Circuit Breaker
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
                var error = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString();
                logger.LogError(
                    "[Polly] Circuit BREAKER OPENED for {Duration}s | Last error: {Error} | URL: {Url}",
                    breakDuration.TotalSeconds, error, request.RequestUri);
            },
            onReset: () =>
            {
                logger.LogInformation("[Polly] Circuit BREAKER CLOSED — CatalogService is healthy again");
            },
            onHalfOpen: () =>
            {
                logger.LogWarning("[Polly] Circuit HALF-OPEN — testing CatalogService connection...");
            });
});

builder.Services.AddEndpointsApiExplorer();

//Swagger
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Prometheus endpoint
app.UseHttpMetrics(); // Middleware để ghi lại request-level metrics
app.MapMetrics();     // Endpoint /metrics cho Prometheus scrape

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
})
.RequireAuthorization()
.WithSummary("Get items in basket")
.WithTags("Basket");



app.MapPost("/api/basket", async (
    HttpContext ctx,
    BasketItem item,
    IBasketRepository repo,
    ICatalogServiceClient catalog,
    IValidator<BasketItem> validator,
    CancellationToken ct) =>
{
    // ✅ Validate input
    FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(item, ct);
    if (!result.IsValid)
    {
        return Results.ValidationProblem(result.ToDictionary());
    }

    var userId = ctx.GetUserId();

    var product = await catalog.GetProductAsync(item.ProductId, ct);
    if (product == null)
        return Results.BadRequest("Product invalid");

    // override data from Catalog
    item.Name = product.Name;
    item.Price = product.Price;

    await repo.AddOrUpdateItemAsync(userId, item, ct);
    return Results.Ok();
})
.RequireAuthorization()
.WithSummary("Add or update item in basket")
.WithTags("Basket");

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
})
.RequireAuthorization()
.WithSummary("Checkout items in basket")
.WithTags("Basket");

app.MapDelete("/api/basket/{productId}", async (
    HttpContext ctx,
    string productId,
    IBasketRepository repo,
    CancellationToken ct) =>
{
    var userId = ctx.GetUserId();
    var success = await repo.RemoveItemAsync(userId, productId, ct);

    return success ? Results.NoContent() : Results.NotFound();
})
.RequireAuthorization()
.WithSummary("Delete item in basket")
.WithTags("Basket");

app.Run();
