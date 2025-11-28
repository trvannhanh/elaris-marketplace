using BuildingBlocks.Contracts.Events;
using LiteDB;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Services.CatalogService.Data;
using Services.CatalogService.Extensions;
using Services.CatalogService.Models;
using Services.CatalogService.Services;

namespace Services.CatalogService.Features.Products.CreateProduct
{
    /// <summary>
    /// Upload product - Chỉ SELLER hoặc ADMIN
    /// </summary>
    public static class CreateProductEndpoint
    {
        public static void MapCreateProduct(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/products", async (HttpContext ctx, MongoContext db, [FromForm] CreateProductRequest request, IFileStorageService fileStorage, IPublishEndpoint publisher) =>
            {
                var userId = ctx.GetUserId();

                var product = new Product
                {
                    SellerId = userId,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Category = request.Category,
                    Status = ProductStatus.PendingApproval,
                    CreatedAt = DateTime.UtcNow
                };

                if (request.ProductFile != null)
                {
                    product.ProductFileUrl = await fileStorage.UploadAsync(
                        request.ProductFile, "products", "files");
                }

                // Upload ảnh preview
                if (request.PreviewImage != null)
                {
                    product.PreviewImageUrl = await fileStorage.UploadAsync(
                        request.PreviewImage, "previews", "images");
                }


                await db.Products.InsertOneAsync(product);

                await publisher.Publish(new ProductCreatedEvent(
                    product.Id!,
                    product.SellerId,
                    //product.Name,
                    request.Quantity,
                    request.LowStockThreshold,
                    //product.Price,
                    product.CreatedAt
                ));

                Log.Information("Product created: {ProductId} by Seller {SellerId}", product.Id, userId);
                return Results.Created($"/api/products/{product.Id}", product);
            })
            .RequireAuthorization("SellerOrAdmin")
            .DisableAntiforgery() // QUAN TRỌNG khi dùng [FromForm] + file upload
            .Accepts<CreateProductRequest>("multipart/form-data")
            .Produces<Product>(201)
            .ProducesValidationProblem()
            .WithName("CreateProduct")
            .WithTags("Products")
            .WithSummary("Create new product with file upload")
            .WithDescription("""
                - Chỉ người bán (Seller) hoặc Admin mới có thể tạo sản phẩm. Hỗ trợ file tài nguyên của sản phẩm ảo và hình ảnh preview
                """)
            .WithOpenApi();

        }

    }
}
