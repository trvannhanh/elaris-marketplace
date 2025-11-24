using BuildingBlocks.Contracts.Events;
using MassTransit;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;
using Services.CatalogService.Data;
using Services.CatalogService.Extensions;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.DeleteProduct
{
    public static class DeleteProductEndpoint
    {
        /// <summary>
        /// Delete product - SELLER (product của mình) hoặc ADMIN
        /// </summary>
        public static void MapDeleteProduct(this IEndpointRouteBuilder app)
        {
            app.MapDelete("/api/products/{id}", async (HttpContext ctx, MongoContext db, IPublishEndpoint publisher, string id) =>
            {
                var userId = ctx.GetUserId();
                var userRole = ctx.GetRole();

                var existing = await db.Products.FindOneAsync(p => p.Id == id);
                if (existing == null) return Results.NotFound();

                if (userRole != "admin")
                {
                    if (existing.SellerId != userId) return Results.Forbid();
                }

                var update = Builders<Product>.Update.Set(p => p.IsDeleted, true);
                var result = await db.Products.UpdateOneAsync(x => x.Id == id, update);

                if (result.ModifiedCount > 0)
                {
                    await publisher.Publish(new ProductDeletedEvent(id, DateTime.UtcNow));
                    return Results.NoContent();
                }

                return Results.BadRequest();
            })
            .RequireAuthorization("SellerOrAdmin")
            .WithName("DeleteProduct")
            .WithTags("Products")
            .WithSummary("Soft delete product")
            .WithDescription("""
                - Seller xóa (soft delete isDelete = true) cho sản phẩm của mình và Admin cho tất cả sản phẩm
                """);
        }
    }
}
