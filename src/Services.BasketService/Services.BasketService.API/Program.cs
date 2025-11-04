using StackExchange.Redis;
using Services.BasketService.Infrastructure.Repositories;
using Services.BasketService.Application.Interfaces;
using Services.BasketService.Application.Models;
using Services.BasketService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("redis:6379"));

builder.Services.AddScoped<IBasketRepository, BasketRepository>();

// ✅ Catalog Service HTTP Client
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri("http://catalogservice:8080");
    // tránh bị time-out
    client.Timeout = TimeSpan.FromSeconds(3);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// API
app.MapGet("/api/basket/{userId}", async (string userId, IBasketRepository repo)
    => Results.Ok(await repo.GetBasketAsync(userId)));

app.MapPost("/api/basket/{userId}", async (string userId, BasketItem item,
                                          IBasketRepository repo,
                                          ICatalogServiceClient catalog,
                                          CancellationToken ct) =>
{
    // ✅ Validate product từ Catalog
    var product = await catalog.GetProductAsync(item.ProductId, ct);

    if (product == null)
        return Results.BadRequest($"ProductId {item.ProductId} invalid");

    // ✅ Sync lại data để tránh fake giá trị
    item.Name = product.Name;
    item.Price = product.Price;

    await repo.AddOrUpdateItemAsync(userId, item, ct);
    return Results.Ok();
});

app.MapDelete("/api/basket/{userId}", async (string userId, IBasketRepository repo) =>
{
    await repo.ClearBasketAsync(userId);
    return Results.NoContent();
});

app.Run();
