using MongoDB.Bson;
using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Extensions;
using Services.CatalogService.Features.Products.GetProducts;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.GetMyProducts
{
    public static class GetMyProductsEndpoint
    {
        /// <summary>
        /// Get my products - SELLER xem sản phẩm của mình
        /// </summary>
        public static void MapGetMyProducts(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/products/my-products", async (HttpContext ctx, [AsParameters] GetProductsQuery query, MongoContext db) =>
            {
                var sellerId = ctx.GetUserId();

                var filterBuilder = Builders<Product>.Filter;
                var filter = filterBuilder.And(
                    filterBuilder.Eq(p => p.IsDeleted, false),
                    filterBuilder.Eq(p => p.SellerId, sellerId)
                );

                // 🔍 Fulltext search
                if (!string.IsNullOrEmpty(query.Search))
                {
                    var textFilter = filterBuilder.Or(
                        filterBuilder.Regex(p => p.Name, new BsonRegularExpression(query.Search, "i")),
                        filterBuilder.Regex(p => p.Description, new BsonRegularExpression(query.Search, "i"))
                    );
                    filter &= textFilter;
                }

                // 💰 Price range
                if (query.MinPrice.HasValue)
                    filter &= filterBuilder.Gte(p => p.Price, query.MinPrice.Value);
                if (query.MaxPrice.HasValue)
                    filter &= filterBuilder.Lte(p => p.Price, query.MaxPrice.Value);

                // 🧾 Sorting
                var sortBuilder = Builders<Product>.Sort;
                var sortField = query.SortBy?.ToLowerInvariant() ?? "createdat";
                var isAsc = query.SortOrder?.ToLowerInvariant() == "asc";
                var sort = isAsc
                    ? sortBuilder.Ascending(sortField)
                    : sortBuilder.Descending(sortField);

                // 📄 Paging
                var skip = (query.Page - 1) * query.PageSize;
                var total = await db.Products.CountDocumentsAsync(filter);
                var items = await db.Products
                    .Find(filter)
                    .Sort(sort)
                    .Skip(skip)
                    .Limit(query.PageSize)
                    .ToListAsync();

                var result = new
                {
                    query.Page,
                    query.PageSize,
                    Total = total,
                    TotalPages = (int)Math.Ceiling(total / (double)query.PageSize),
                    Items = items
                };

                return Results.Ok(result);
            })
            .RequireAuthorization("Seller")
            .WithName("GetMyProducts")
            .WithTags("Products")
            .WithSummary("Get my products with search, filter, sort, pagination")
            .WithDescription("""
                - Seller xem danh sách những product của mình
                - Sử dụng bộ lọc với từ khóa sẽ lọc theo Name và Description của sản phẩm
                - Sử dụng bộ theo khoảng giá từ giá bao nhiêu đến giá bao nhiêu
                - Sử dụng bộ lọc Sort với sortField "createdat" để lọc theo ngày tạo sản phẩm và sort "asc"
                """)
            .WithOpenApi();
        }
    }
}
