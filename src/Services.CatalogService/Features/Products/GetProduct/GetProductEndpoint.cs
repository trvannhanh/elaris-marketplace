using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.GetProduct
{
    /// <summary>
    /// View product detail - Ai cũng xem được
    /// </summary>
    public static class GetProductEndpoint
    {
        public static void MapGetProduct(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/products/{id}", async (MongoContext db, string id) =>
            {
                var product = await db.Products.FindOneAsync(x => x.Id == id);

                return product is not null
                    ? Results.Ok(product)
                    : Results.NotFound("Product not found or not available");
            })
            .WithName("GetProduct")
            .WithTags("Products")
            .WithSummary("Get product detail by ID")
            .WithDescription("""
                - Ai cũng xem được sản phẩm đã duyệt (Approved)
                - Ẩn hoàn toàn sản phẩm đã xóa mềm
                """)
            .Produces<Product>(200)
            .Produces(400) // Invalid ID
            .Produces(404);
        }
    }
}
