

namespace Services.PaymentService.Application.DTOs
{
    public class PaymentReconciliationDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalGrossAmount { get; set; }
        public decimal TotalRefundedAmount { get; set; }
        public decimal TotalNetAmount { get; set; }
        public List<DailyBreakdown> DailyBreakdown { get; set; } = new();
    }

    public class DailyBreakdown
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }
}
