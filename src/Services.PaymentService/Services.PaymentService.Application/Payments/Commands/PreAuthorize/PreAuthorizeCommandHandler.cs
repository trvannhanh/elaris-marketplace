using MediatR;
using Microsoft.Extensions.Logging;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.PreAuthorize
{
    public class PreAuthorizeCommandHandler
    : IRequestHandler<PreAuthorizeCommand, PreAuthorizeResult>
    {
        private readonly IUnitOfWork _uow;
        private readonly ILogger<PreAuthorizeCommandHandler> _logger;

        public PreAuthorizeCommandHandler(IUnitOfWork uow, ILogger<PreAuthorizeCommandHandler> logger)
        {
            _uow = uow;
            _logger = logger;
        }

        public async Task<PreAuthorizeResult> Handle(PreAuthorizeCommand request, CancellationToken ct)
        {
            await Task.Delay(150, ct); // mô phỏng mạng

            var authorized = new Random().NextDouble() > 0.2;
            var paymentId = Guid.NewGuid();

            var payment = new Payment
            {
                Id = paymentId,
                OrderId = request.OrderId,
                Amount = request.Amount,
                Status = authorized ? PaymentStatus.Authorized : PaymentStatus.Failed,
                CompletedAt = DateTime.UtcNow
            };

            await _uow.Payment.AddAsync(payment, ct);
            await _uow.SaveChangesAsync(ct);

            return new PreAuthorizeResult(
                Success: authorized,
                PaymentId: paymentId,
                Message: authorized ? "Payment authorized" : "Gateway declined",
                Status: payment.Status
            );
        }
    }
}
