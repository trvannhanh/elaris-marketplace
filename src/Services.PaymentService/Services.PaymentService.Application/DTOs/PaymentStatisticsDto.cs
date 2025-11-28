using System;


namespace Services.PaymentService.Application.DTOs
{
    public class PaymentStatisticsDto
    {
        public int TotalPayments { get; set; }
        public int CompletedPayments { get; set; }
        public int FailedPayments { get; set; }
        public int PendingPayments { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CompletedAmount { get; set; }
        public decimal RefundedAmount { get; set; }
        public decimal AveragePaymentAmount { get; set; }
        public double SuccessRate { get; set; }
    }
}
