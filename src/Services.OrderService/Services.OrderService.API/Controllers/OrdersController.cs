using MediatR;
using Microsoft.AspNetCore.Mvc;
using Services.OrderService.Application.Orders.Commands.CreateOrder;
using Services.OrderService.Application.Orders.GetOrdersWithFilters;
using Services.OrderService.Application.Orders.Queries.GetOrderById;

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

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _mediator.Send(new GetOrderByIdQuery(id));
            return result is not null ? Ok(result) : NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders(
            [FromQuery] string? search,
            [FromQuery] string? productId,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortDirection,
            [FromQuery] int page = 1,  
            [FromQuery] int pageSize = 10)
        {
            var query = new GetOrdersWithFiltersQuery(
                search, productId, sortBy, sortDirection, page, pageSize);

            var result = await _mediator.Send(query);
            return Ok(result);
        }
    }
}
