using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Servers;
using Services.CatalogService.Data;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.GetProducts
{
    /// <summary>
    /// Browse products - Ai cũng xem được (kể cả chưa đăng nhập)
    /// </summary>
    public static class GetProductsEndpoint
    {
        public static void MapGetProducts(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/products", async ([AsParameters] GetProductsQuery query, MongoContext db) =>
            {
                var filterBuilder = Builders<Product>.Filter;
                var filter = filterBuilder.And(
                     filterBuilder.Eq(p => p.IsDeleted, false),
                     filterBuilder.Eq(p => p.Status, ProductStatus.Approved)
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
            .WithName("GetProducts")
            .WithTags("Products")
            .WithSummary("Get products with search, filter, sort, pagination")
            .WithDescription("""
                - Tất cả vai trò đều có thể xem danh sách sản phẩm (chưa xóa isDelete = false và đã được duyệt Approve)
                - Sử dụng bộ lọc với từ khóa sẽ lọc theo Name và Description của sản phẩm
                - Sử dụng bộ theo khoảng giá từ giá bao nhiêu đến giá bao nhiêu
                - Sử dụng bộ lọc Sort với sortField "createdat" để lọc theo ngày tạo sản phẩm và sort "asc"
                """)
            .WithOpenApi();
        }
    }
}
