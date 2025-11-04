using StackExchange.Redis;
using Services.BasketService.Infrastructure.Repositories;
using Services.BasketService.Application.Interfaces;
using Services.BasketService.Application.Models;

var builder = WebApplication.CreateBuilder(args);

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect("redis:6379"));

builder.Services.AddScoped<IBasketRepository, BasketRepository>();

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

app.MapPost("/api/basket/{userId}", async (string userId, BasketItem item, IBasketRepository repo) =>
{
    await repo.AddOrUpdateItemAsync(userId, item);
    return Results.Ok();
});

app.MapDelete("/api/basket/{userId}", async (string userId, IBasketRepository repo) =>
{
    await repo.ClearBasketAsync(userId);
    return Results.NoContent();
});

app.Run();
