using MassTransit;
using MediatR;
using Services.PaymentService.Application.Interfaces;
using Services.PaymentService.Domain.Entities;

namespace Services.PaymentService.Application.Payments.Commands.CreatePayment
{
    public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, Payment>
    {
        private readonly IUnitOfWork _uow;
        private readonly IPublishEndpoint _publisher;

        public CreatePaymentCommandHandler(IUnitOfWork uow, IPublishEndpoint publisher)
        {
            _uow = uow;
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

            await _uow.Payment.AddAsync(payment, ct);
            await _uow.SaveChangesAsync(ct);

            return payment;
        }
    }
}
