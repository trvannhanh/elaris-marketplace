using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Queries.GetPaymentReconciliation
{
    public class GetPaymentReconciliationQueryHandler
    : IRequestHandler<GetPaymentReconciliationQuery, PaymentReconciliationDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetPaymentReconciliationQueryHandler> _logger;

        public GetPaymentReconciliationQueryHandler(
            IUnitOfWork uow,
            ILogger<GetPaymentReconciliationQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PaymentReconciliationDto> Handle(
            GetPaymentReconciliationQuery request,
            CancellationToken cancellationToken)
        {
            var payments = await _uow.Payment.GetQueryable()
                .Where(p => p.CreatedAt >= request.FromDate && p.CreatedAt <= request.ToDate)
                .Where(p => p.Status == PaymentStatus.Completed)
                .ToListAsync(cancellationToken);

            var report = new PaymentReconciliationDto
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                TotalTransactions = payments.Count,
                TotalGrossAmount = payments.Sum(p => p.Amount),
                TotalRefundedAmount = payments.Sum(p => p.RefundedAmount ?? 0)
            };

            report.TotalNetAmount = report.TotalGrossAmount - report.TotalRefundedAmount;

            // Daily breakdown
            report.DailyBreakdown = payments
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new DailyBreakdown
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(p => p.Amount)
                })
                .OrderBy(d => d.Date)
                .ToList();

            _logger.LogInformation(
                "[Reconciliation] Generated report for {FromDate} to {ToDate}. Total: {Total}",
                request.FromDate, request.ToDate, report.TotalGrossAmount);

            return report;
        }
    }
}
