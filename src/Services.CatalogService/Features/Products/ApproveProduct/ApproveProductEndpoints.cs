using MassTransit;
using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Extensions;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.ApproveProduct
{
    public static class ApproveProductEndpoint
    {
        /// <summary>
        /// Approve product - Chỉ ADMIN 
        /// </summary>
        public static void MapApproveProduct(this IEndpointRouteBuilder app)
        {
            app.MapPatch("/api/products/{id}/approve", async (HttpContext ctx, MongoContext db, IPublishEndpoint publisher, string id) =>
            {
                // Lấy luôn cả sản phẩm bị xóa (nếu cần duyệt)
                var existing = await db.Products.FindOneAsync(p => p.Id == id);

                if (existing == null)
                    return Results.NotFound(new { Message = "Product not found." });

                if (existing.Status == ProductStatus.Approved)
                    return Results.BadRequest(new { Message = "Product is already approved." });

                var update = Builders<Product>.Update
                    .Set(p => p.Status, ProductStatus.Approved);

                var result = await db.Products.Raw.UpdateOneAsync(
                    x => x.Id == id,
                    update
                );

                if (result.ModifiedCount == 0)
                    return Results.BadRequest(new { Message = "Approve failed." });

                // Publish event nếu bạn có event bus
                // await publisher.Publish(new ProductApprovedEvent { ProductId = id });

                existing.Status = ProductStatus.Approved;

                return Results.Ok(existing);
            })
            .RequireAuthorization("Admin")
            .WithName("ApproveProduct")
            .WithTags("Products")
            .WithSummary("Approve product")
            .WithDescription("""
                - Admin chấp nhận (Approve) duyệt cho sản phẩm đang đợi được duyệt (Pending)
                """);
        }
    }
}
