

namespace Services.BasketService.Application.Models
{
    public class BasketDto
    {
        public string UserId { get; set; } = default!;
        public List<BasketItem> Items { get; set; } = new();
        public decimal TotalPrice => Items.Sum(i => i.Price * i.Quantity);
    }
}
