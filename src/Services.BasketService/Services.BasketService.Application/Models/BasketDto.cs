

namespace Services.BasketService.Application.Models
{
    // ==================== REQUEST DTOs ====================

    public class AddBasketItemRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class UpdateBasketItemRequest
    {
        public int Quantity { get; set; }
    }

    public class CheckoutRequest
    {
        public string? Note { get; set; }
        public string? VoucherCode { get; set; }
    }

    // ==================== RESPONSE DTOs ====================

    public class BasketDto
    {
        public string UserId { get; set; } = string.Empty;
        public List<BasketItemDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Total { get; set; }
        public int TotalItems { get; set; }
        //public decimal TotalPrice => Items.Sum(i => i.Price * i.Quantity);
        public DateTime? LastUpdated { get; set; }
    }

    public class BasketItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal => Price * Quantity;
        public string? ImageUrl { get; set; }
    }

    public class BasketSummaryDto
    {
        public int TotalItems { get; set; }
        public decimal Total { get; set; }
    }

    public class CheckoutResultDto
    {
        public bool Success { get; set; }
        public string? OrderId { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
