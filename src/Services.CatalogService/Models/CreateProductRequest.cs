namespace Services.CatalogService.Models
{
    public record CreateProductRequest(
        string Name,
        string Description,
        decimal Price,
        string Category,
        int Quantity,
        int LowStockThreshold,
        IFormFile ProductFile,           // File sản phẩm (zip, pdf, video...)
        IFormFile? PreviewImage = null   // Ảnh xem trước
    );
}
