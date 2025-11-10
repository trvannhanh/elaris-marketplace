namespace Services.CatalogService.Features.Products.GetProducts
{
    public record GetProductsQuery(
        string? Search = null,
        decimal? MinPrice = null,
        decimal? MaxPrice = null,
        string? SortBy = "createdAt",
        string? SortOrder = "desc",
        int Page = 1,
        int PageSize = 10
    );
}
