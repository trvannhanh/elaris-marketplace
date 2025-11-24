namespace Services.CatalogService.Models
{
    public record UpdateProductRequest(
        string? Name,
        string? Description,
        decimal? Price,
        string? Category,
        IFormFile? ProductFile,      // có thể thay file mới
        IFormFile? PreviewImage      // có thể thay ảnh mới
    );
}
