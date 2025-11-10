using MongoDB.Bson;
using MongoDB.Driver;
using Services.CatalogService.Data;
using Services.CatalogService.Models;

namespace Services.CatalogService.Features.Products.GetProducts
{
    public static class GetProductsEndpoint
    {
        public static void MapGetProducts(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/products", async ([AsParameters] GetProductsQuery query, MongoContext db) =>
            {
                var filterBuilder = Builders<Product>.Filter;
                var filter = filterBuilder.Eq(p => p.IsDeleted, false);

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
            .WithOpenApi();
        }
    }
}
