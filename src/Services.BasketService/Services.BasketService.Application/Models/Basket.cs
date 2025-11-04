

namespace Services.BasketService.Application.Models
{
    public class Basket
    {
        public string UserId { get; set; } = default!;
        public List<BasketItem> Items { get; set; } = new();
    }
}
