
namespace Services.BasketService.Application.Models
{
    public class BasketItem
    {
        public string ProductId { get; set; } = default!;
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; } = default!;
    }
}
