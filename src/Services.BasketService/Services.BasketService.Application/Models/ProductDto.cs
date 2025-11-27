

namespace Services.BasketService.Application.Models
{
    public class ProductDto
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public string? PreviewImageUrl { get; set; }

    }
}
