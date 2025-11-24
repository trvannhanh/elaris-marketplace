using MassTransit;
using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.RejectProduct
{
    public static class RejectProductEndpoint
    {
        /// <summary>
        /// Reject product - Chỉ ADMIN 
        /// </summary>
        public static void MapRejectProduct(this IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/products/{id}/reject", async (HttpContext ctx, MongoContext db, IPublishEndpoint publisher, string id) =>
            {
                // Lấy luôn cả bị xóa
                var existing = await db.Products.FindOneAsync(p => p.Id == id);

                if (existing == null)
                    return Results.NotFound(new { Message = "Product not found." });

                if (existing.Status == ProductStatus.Rejected)
                    return Results.BadRequest(new { Message = "Product is already rejected." });

                var update = Builders<Product>.Update
                    .Set(p => p.Status, ProductStatus.Rejected);

                var result = await db.Products.Raw.UpdateOneAsync(
                    p => p.Id == id,
                    update
                );

                if (result.ModifiedCount == 0)
                    return Results.BadRequest(new { Message = "Reject failed." });

                // Publish event nếu có
                // await publisher.Publish(new ProductRejectedEvent { ProductId = id });

                existing.Status = ProductStatus.Rejected;

                return Results.Ok(existing);
            })
            .RequireAuthorization("Admin")
            .WithName("RejectProduct")
            .WithTags("Products")
            .WithSummary("Reject product")
            .WithDescription("""
                - Admin từ chối (Reject) duyệt cho sản phẩm đang đợi được duyệt (Pending)
                """);
        }
    }
}
