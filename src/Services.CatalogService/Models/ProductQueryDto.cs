namespace Services.CatalogService.Models
{
    public class ProductQueryDto
    {
        public string? Search { get; set; } // fulltext tìm theo Name/Description
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }

        public string? SortBy { get; set; } = "createdAt"; // name, price, createdAt
        public string? SortOrder { get; set; } = "desc";   // asc | desc

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
