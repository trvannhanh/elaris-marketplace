using BuildingBlocks.Contracts.Events;
using MassTransit;
using Serilog;
using Services.CatalogService.Data;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.CreateProduct
{
    public static class CreateProductEndpoint
    {
        public static void MapCreateProduct(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/products", async (MongoContext db, Product p, IPublishEndpoint publisher) =>
            {
                await db.Products.InsertOneAsync(p);

                await publisher.Publish(new ProductCreatedEvent(
                    p.Id!,
                    p.Name,
                    p.Price,
                    p.CreatedAt
                ));

                Log.Information("✅ ProductCreatedEvent Published for {ProductId}", p.Id);
                return Results.Created($"/api/products/{p.Id}", p);
            })
            .RequireAuthorization("AdminOnly")
            .WithName("CreateProduct")
            .WithTags("Products")
            .WithSummary("Create new product");
        }
    }
}
