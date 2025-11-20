using Grpc.Core;
using MediatR;
using Services.PaymentService.Application.Payments.Commands.PreAuthorize;
using static MassTransit.ValidationResultExtensions;
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

        public override Task<CheckCardResponse> CheckCard(CheckCardRequest request, ServerCallContext context)
        {
            // Kiểm tra thẻ ở đây: gọi internal service / gateway / db
            // Simulate:
            bool valid = true;
            bool blocked = false;
            bool sufficient = request.Amount <= 30000; // fake rule

            _logger.LogInformation("====== Card Check Result for User {UserId}: valid {valid}, blocked {blocked}, sufficient {sufficient}",
                                    request.UserId, valid, blocked, sufficient);

            var msg = valid && !blocked && sufficient ? "✅ OK" : "❌ Not allowed";

            return Task.FromResult(new CheckCardResponse
            {
                Valid = valid,
                Blocked = blocked,
                SufficientLimit = sufficient,
                Message = msg
            });
        }
    }
}
