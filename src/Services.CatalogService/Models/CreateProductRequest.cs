namespace Services.CatalogService.Models
{
    public record CreateProductRequest(
        string Name,
        string Description,
        decimal Price,
        string Category,
        IFormFile ProductFile,           // File sản phẩm (zip, pdf, video...)
        IFormFile? PreviewImage = null   // Ảnh xem trước
    );
}
