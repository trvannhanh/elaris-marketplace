using MassTransit;
using MediatR;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands
{
    public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, Payment>
    {
        private readonly IPaymentRepository _repo;
        private readonly IPublishEndpoint _publisher;

        public CreatePaymentCommandHandler(IPaymentRepository repo, IPublishEndpoint publisher)
        {
            _repo = repo;
            _publisher = publisher;
        }

        public async Task<Payment> Handle(CreatePaymentCommand request, CancellationToken ct)
        {
            var payment = new Payment
            {
                OrderId = request.OrderId,
                Amount = request.Amount,
                Status = PaymentStatus.Pending
            };

            await _repo.AddAsync(payment, ct);
            await _repo.SaveChangesAsync(ct);

            return payment;
        }
    }
}
