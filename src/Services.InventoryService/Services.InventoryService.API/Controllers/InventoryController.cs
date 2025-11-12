using MediatR;
using Microsoft.AspNetCore.Mvc;
using Services.InventoryService.Application.Inventory.Commands.CreateOrUpdateInventoryItem;
using Services.InventoryService.Application.Inventory.Commands.UpdateStock;
using Services.InventoryService.Application.Inventory.Queries.GetInventoryByProductId;

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

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdate([FromBody] CreateOrUpdateInventoryItemCommand command)
        {
            await _mediator.Send(command);
            return Ok(new { message = "Inventory item created or updated." });
        }

        [HttpPatch("{productId}/increase/{quantity:int}")]
        public async Task<IActionResult> IncreaseStock(string productId, int quantity)
        {
            await _mediator.Send(new UpdateStockCommand(productId, -quantity)); // Negative => add
            return Ok(new { message = $"Increased stock for {productId} by {quantity}" });
        }

        [HttpPatch("{productId}/decrease/{quantity:int}")]
        public async Task<IActionResult> DecreaseStock(string productId, int quantity)
        {
            await _mediator.Send(new UpdateStockCommand(productId, quantity));
            return Ok(new { message = $"Decreased stock for {productId} by {quantity}" });
        }


    }
}
