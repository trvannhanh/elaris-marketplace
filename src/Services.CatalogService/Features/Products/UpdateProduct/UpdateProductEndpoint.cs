using Services.CatalogService.Data;
using Services.CatalogService.Models;
using MongoDB.Driver;
using BuildingBlocks.Contracts.Events;
using MassTransit;
using Services.CatalogService.Extensions;
using Services.CatalogService.Services;

namespace Services.CatalogService.Features.Products.UpdateProduct
{
    public static class UpdateProductEndpoint
    {
        /// <summary>
        /// Update product - Chỉ SELLER (product của mình) hoặc ADMIN
        /// </summary>
        public static void MapUpdateProduct(this IEndpointRouteBuilder app)
        {
            app.MapPut("/api/products/{id}", async (HttpContext ctx, MongoContext db, IFileStorageService fileStorage, IPublishEndpoint publisher, string id, UpdateProductRequest request) =>
            {

                var userId = ctx.GetUserId();
                var userRole = ctx.GetRole();
                var existing = await db.Products.FindOneAsync(p => p.Id == id);
                if (existing == null)
                    return Results.NotFound("Product not found or deleted");

                // === QUYỀN: Chỉ admin hoặc owner mới được sửa ===
                if (userRole != "admin" && existing.SellerId != userId)
                    return Results.Forbid();

                // === Chỉ update những field có gửi lên ===
                if (request.Name != null) existing.Name = request.Name;
                if (request.Description != null) existing.Description = request.Description;
                if (request.Price.HasValue) existing.Price = request.Price.Value;
                if (request.Category != null) existing.Category = request.Category;

                // === Upload file mới (nếu có) ===
                if (request.ProductFile != null)
                {
                    existing.ProductFileUrl = await fileStorage.UploadAsync(
                        request.ProductFile, "products", "files");
                }

                if (request.PreviewImage != null)
                {
                    existing.PreviewImageUrl = await fileStorage.UploadAsync(
                        request.PreviewImage, "previews", "images");
                }


                var result = await db.Products.ReplaceOneAsync(p => p.Id == id, existing);

                if (result.ModifiedCount == 0)
                    return Results.BadRequest("Update failed");

                // === Publish event ===
                await publisher.Publish(new ProductUpdatedEvent(
                    existing.Id!,
                    existing.Name,
                    existing.Description,
                    existing.Price,
                    DateTime.UtcNow
                ));

                return Results.Ok(existing);
            })
            .RequireAuthorization("SellerOrAdmin")
            .DisableAntiforgery() // BẮT BUỘC khi dùng IFormFile
            .Accepts<UpdateProductRequest>("multipart/form-data")
            .Produces<Product>(200)
            .Produces(403)
            .Produces(404)
            .WithName("UpdateProduct")
            .WithTags("Products")
            .WithSummary("Update product (partial) - Support file replacement")
            .WithDescription("""
                - Chỉ cần gửi các field muốn thay đổi. File cũ sẽ được giữ nguyên nếu không gửi file mới.
            """);

        }
    }
}
