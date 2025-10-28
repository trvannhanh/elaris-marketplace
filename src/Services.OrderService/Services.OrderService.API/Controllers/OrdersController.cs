using MediatR;
using Microsoft.AspNetCore.Mvc;
using Services.OrderService.Application.Orders.Commands.CreateOrder;

namespace Services.OrderService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder(CreateOrderCommand command)
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpGet("{id}")]
        public IActionResult GetById(Guid id)
        {
            return Ok($"Order {id}");
        }
    }
}
