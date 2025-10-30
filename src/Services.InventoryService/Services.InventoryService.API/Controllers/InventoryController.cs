using MediatR;
using Microsoft.AspNetCore.Mvc;
using Services.InventoryService.Application.Inventory.Queries;

namespace Services.InventoryService.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IMediator _mediator;

        public InventoryController(IMediator mediator)
        {
            _mediator = mediator;
        }


        [HttpGet("{productId}")]
        public async Task<IActionResult> GetByProductId(string productId)
        {
            var result = await _mediator.Send(new GetInventoryByProductIdQuery(productId));

            if (result is null)
                return NotFound(new { message = "Product not found in inventory" });

            return Ok(result);
        }


    }
}
