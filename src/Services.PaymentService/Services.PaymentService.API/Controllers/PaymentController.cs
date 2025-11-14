using MediatR;
using Microsoft.AspNetCore.Mvc;
using Services.PaymentService.Application.Payments.Commands.CreatePayment;

namespace Services.PaymentService.API.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PaymentController(IMediator mediator) => _mediator = mediator;

        [HttpPost("preauthorize")]
        public async Task<IActionResult> PreAuthorize([FromBody] CreatePaymentCommand cmd, CancellationToken ct)
        {
            var payment = await _mediator.Send(cmd, ct);
            return Ok(new { payment.Id, payment.Status, payment.OrderId });
        }
    }
}
