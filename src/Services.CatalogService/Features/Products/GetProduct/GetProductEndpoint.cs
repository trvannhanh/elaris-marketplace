using MongoDB.Driver;
using Services.CatalogService.Data;

namespace Services.CatalogService.Features.Products.GetProduct
{
    public static class GetProductEndpoint
    {
        public static void MapGetProduct(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/products/{id}", async (MongoContext db, string id) =>
            {
                var product = await db.Products.FindOneAsync(x => x.Id == id);

                return product is not null
                    ? Results.Ok(product)
                    : Results.NotFound();
            })
            .WithName("GetProduct")
            .WithTags("Products")
            .WithSummary("Get product by ID");
        }
    }
}
