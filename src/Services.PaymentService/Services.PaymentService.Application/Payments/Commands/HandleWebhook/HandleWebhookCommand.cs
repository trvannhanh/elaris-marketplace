using MediatR;

namespace Services.PaymentService.Application.Payments.Commands.HandleWebhook
{
    public record HandleWebhookCommand(
         string Signature,
         Dictionary<string, string> Data
     ) : IRequest<bool>;
}
