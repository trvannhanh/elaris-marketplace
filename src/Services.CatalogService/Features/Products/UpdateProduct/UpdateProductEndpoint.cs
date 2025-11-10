using Services.CatalogService.Data;
using Services.CatalogService.Models;
using MongoDB.Driver;

namespace Services.CatalogService.Features.Products.UpdateProduct
{
    public static class UpdateProductEndpoint
    {
        public static void MapUpdateProduct(this IEndpointRouteBuilder app)
        {
            app.MapPut("/api/products/{id}", async (MongoContext db, string id, Product updated) =>
            {
                var result = await db.Products.ReplaceOneAsync(x => x.Id == id, updated);
                return result.ModifiedCount > 0 ? Results.Ok(updated) : Results.NotFound();
            })
            .RequireAuthorization("AdminOnly")
            .WithName("UpdateProduct")
            .WithTags("Products")
            .WithSummary("Update product by ID");
        }
    }
}
