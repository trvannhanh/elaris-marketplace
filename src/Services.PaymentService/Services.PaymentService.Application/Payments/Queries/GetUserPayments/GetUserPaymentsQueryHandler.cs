

using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Application.Models;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Queries.GetUserPayments
{
    public class GetUserPaymentsQueryHandler
    : IRequestHandler<GetUserPaymentsQuery, PaginatedList<PaymentDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetUserPaymentsQueryHandler> _logger;

        public GetUserPaymentsQueryHandler(
            IUnitOfWork uow,
            ILogger<GetUserPaymentsQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PaginatedList<PaymentDto>> Handle(
            GetUserPaymentsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _uow.Payment.GetQueryable()
                .Where(p => p.UserId == request.UserId);

            // Filter by status
            if (request.Status.HasValue)
            {
                query = query.Where(p => p.Status == request.Status.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var payments = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = payments.Select(MapToDto).ToList();

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
                ProcessedAt = payment.ProcessedAt,
                CapturedAt = payment.CapturedAt,
                RefundedAt = payment.RefundedAt,
                RefundedAmount = payment.RefundedAmount
            };
        }
    }
}
