namespace Services.InventoryService.Application.DTOs
{
    public class ProductDto
    {
        public string Id { get; set; } = default!;
        public string SellerId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public string? PreviewImageUrl { get; set; }

    }
}
