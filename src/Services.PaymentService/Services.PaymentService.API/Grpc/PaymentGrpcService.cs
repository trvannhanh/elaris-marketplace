using Grpc.Core;
using MediatR;
using Services.PaymentService.Application.Payments.Commands.PreAuthorize;
using static Services.PaymentService.PaymentService;

namespace Services.PaymentService.API.Grpc
{
    public class PaymentGrpcService : PaymentServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PaymentGrpcService> _logger;

        public PaymentGrpcService(IMediator mediator, ILogger<PaymentGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override Task<PreAuthorizeResponse> PreAuthorize(
            PreAuthorizeRequest request,
            ServerCallContext context)
        {
            // Convert từ string → Guid, decimal
            var orderId = Guid.Parse(request.OrderId);
            var amount = decimal.Parse(request.Amount);

            var command = new PreAuthorizeCommand(orderId, amount, request.UserId);
            var result = _mediator.Send(command, context.CancellationToken).GetAwaiter().GetResult();

            return Task.FromResult(new PreAuthorizeResponse
            {
                Success = result.Success,
                PaymentId = result.PaymentId.ToString(),  // Guid → string
                Message = result.Message,
                Status = result.Status.ToString()
            });
        }
    }
}
