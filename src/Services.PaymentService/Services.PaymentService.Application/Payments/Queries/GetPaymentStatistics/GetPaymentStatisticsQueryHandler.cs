using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Queries.GetPaymentStatistics
{
    public class GetPaymentStatisticsQueryHandler
    : IRequestHandler<GetPaymentStatisticsQuery, PaymentStatisticsDto>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetPaymentStatisticsQueryHandler> _logger;

        public GetPaymentStatisticsQueryHandler(
            IUnitOfWork uow,
            ILogger<GetPaymentStatisticsQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PaymentStatisticsDto> Handle(
            GetPaymentStatisticsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _uow.Payment.GetQueryable();

            // Filter by user if specified
            if (!string.IsNullOrEmpty(request.UserId))
            {
                query = query.Where(p => p.UserId == request.UserId);
            }

            // Date range filter
            if (request.FromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= request.ToDate.Value);
            }

            var payments = await query.ToListAsync(cancellationToken);

            var stats = new PaymentStatisticsDto
            {
                TotalPayments = payments.Count,
                CompletedPayments = payments.Count(p => p.Status == PaymentStatus.Completed),
                FailedPayments = payments.Count(p => p.Status == PaymentStatus.Failed),
                PendingPayments = payments.Count(p => p.Status == PaymentStatus.Pending),
                TotalAmount = payments.Sum(p => p.Amount),
                CompletedAmount = payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount),
                RefundedAmount = payments.Sum(p => p.RefundedAmount ?? 0),
                AveragePaymentAmount = payments.Any() ? payments.Average(p => p.Amount) : 0
            };

            // Calculate success rate
            var processedPayments = payments.Count(p =>
                p.Status == PaymentStatus.Completed || p.Status == PaymentStatus.Failed);

            stats.SuccessRate = processedPayments > 0
                ? (double)stats.CompletedPayments / processedPayments * 100
                : 0;

            _logger.LogInformation(
                "[GetStats] Total: {Total}, Completed: {Completed}, Failed: {Failed}, Success Rate: {Rate}%",
                stats.TotalPayments, stats.CompletedPayments, stats.FailedPayments, stats.SuccessRate);

            return stats;
        }
    }
}
