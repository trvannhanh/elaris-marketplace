

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Application.Models;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Queries.GetFailedPayments
{
    public class GetFailedPaymentsQueryHandler
    : IRequestHandler<GetFailedPaymentsQuery, PaginatedList<PaymentDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetFailedPaymentsQueryHandler> _logger;

        public GetFailedPaymentsQueryHandler(
            IUnitOfWork uow,
            ILogger<GetFailedPaymentsQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PaginatedList<PaymentDto>> Handle(
            GetFailedPaymentsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _uow.Payment.GetQueryable()
                .Where(p => p.Status == PaymentStatus.Failed);

            if (request.FromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt <= request.ToDate.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = payments.Select(MapToDto).ToList();

            _logger.LogInformation("[GetFailedPayments] Found {Count} failed payments", totalCount);

            return new PaginatedList<PaymentDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.Page,
                PageSize = request.PageSize
            };
        }

        private PaymentDto MapToDto(Payment payment)
        {
            return new PaymentDto
            {
                Id = payment.Id,
                OrderId = payment.OrderId,
                UserId = payment.UserId,
                Amount = payment.Amount,
                Status = payment.Status.ToString(),
                TransactionId = payment.TransactionId,
                FailureReason = payment.FailureReason,
                CreatedAt = payment.CreatedAt,
                ProcessedAt = payment.ProcessedAt
            };
        }
    }
}
