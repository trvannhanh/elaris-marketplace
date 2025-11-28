using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Application.Models;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Queries.GetAllPayments
{
    public class GetAllPaymentsQueryHandler
    : IRequestHandler<GetAllPaymentsQuery, PaginatedList<PaymentDto>>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<GetAllPaymentsQueryHandler> _logger;

        public GetAllPaymentsQueryHandler(
            IUnitOfWork uow,
            ILogger<GetAllPaymentsQueryHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PaginatedList<PaymentDto>> Handle(
            GetAllPaymentsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _uow.Payment.GetQueryable();

            // Filter by user
            if (!string.IsNullOrEmpty(request.UserId))
            {
                query = query.Where(p => p.UserId == request.UserId);
            }

            // Filter by status
            if (request.Status.HasValue)
            {
                query = query.Where(p => p.Status == request.Status.Value);
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

            // Search by transaction ID
            if (!string.IsNullOrEmpty(request.Search))
            {
                query = query.Where(p =>
                    p.TransactionId != null && p.TransactionId.Contains(request.Search));
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
                RefundedAmount = payment.RefundedAmount,
                RefundReason = payment.RefundReason
            };
        }
    }
}
