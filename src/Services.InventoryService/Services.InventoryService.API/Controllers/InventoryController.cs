using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.InventoryService.API.Extensions;
using Services.InventoryService.Application.Inventory.Commands.BulkUpdateInventory;
using Services.InventoryService.Application.Inventory.Commands.ConfirmStockDeduction;
using Services.InventoryService.Application.Inventory.Commands.CreateOrUpdateInventoryItem;
using Services.InventoryService.Application.Inventory.Commands.DecreaseStock;
using Services.InventoryService.Application.Inventory.Commands.IncreaseStock;
using Services.InventoryService.Application.Inventory.Commands.ReleaseStock;
using Services.InventoryService.Application.Inventory.Commands.ReserveStock;
using Services.InventoryService.Application.Inventory.Commands.SetLowStockThreshold;
using Services.InventoryService.Application.Inventory.Queries.CheckProductsAvailability;
using Services.InventoryService.Application.Inventory.Queries.GetInventoryByProductId;
using Services.InventoryService.Application.Inventory.Queries.GetInventoryHistory;
using Services.InventoryService.Application.Inventory.Queries.GetInventoryList;
using Services.InventoryService.Application.Inventory.Queries.GetInventoryStatistics;
using Services.InventoryService.Application.Inventory.Queries.GetLowStockItems;
using Services.InventoryService.Domain.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace Services.InventoryService.API.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(IMediator mediator, ILogger<InventoryController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // ==================== QUERY ENDPOINTS ====================

        /// <summary>
        /// Lấy thông tin tồn kho theo ProductId - Public endpoint
        /// </summary>
        [HttpGet("{productId}")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Lấy thông tin tồn kho theo ProductId",
            Description = "Bất kỳ ai cũng có thể xem số lượng tồn kho của sản phẩm."
        )]
        public async Task<IActionResult> GetByProductId(string productId)
        {
            _logger.LogInformation("[GetInventory] Fetching inventory for product {ProductId}", productId);

            var result = await _mediator.Send(new GetInventoryByProductIdQuery(productId));

            if (result is null)
            {
                _logger.LogWarning("[GetInventory] Product {ProductId} not found in inventory", productId);
                return NotFound(new { Message = "Product not found in inventory" });
            }

            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách tồn kho với filters - Admin/Seller
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "SellerOrAdmin")]
        [SwaggerOperation(
            Summary = "Lấy danh sách tồn kho với filters",
            Description = "Seller xem sản phẩm của mình, Admin xem tất cả. Hỗ trợ lọc theo trạng thái tồn kho."
        )]
        public async Task<IActionResult> GetInventoryList(
            [FromQuery] string? search,
            [FromQuery] string? sellerId,
            [FromQuery] InventoryStatus? status,
            [FromQuery] bool? lowStock,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = HttpContext.GetUserId();
            var userRole = HttpContext.GetRole();

            // Seller chỉ xem inventory của mình
            if (userRole == "seller")
            {
                sellerId = userId;
            }

            var query = new GetInventoryListQuery(
                search, sellerId, status, lowStock, page, pageSize);

            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Lấy sản phẩm sắp hết hàng - Admin/Seller
        /// </summary>
        [HttpGet("low-stock")]
        [Authorize(Policy = "SellerOrAdmin")]
        [SwaggerOperation(
            Summary = "Lấy danh sách sản phẩm sắp hết hàng",
            Description = "Seller xem sản phẩm của mình đang sắp hết hàng, Admin xem tất cả."
        )]
        public async Task<IActionResult> GetLowStockItems(
            [FromQuery] int threshold = 10,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = HttpContext.GetUserId();
            var userRole = HttpContext.GetRole();

            var query = new GetLowStockItemsQuery(
                userRole == "seller" ? userId : null,
                threshold,
                page,
                pageSize);

            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Kiểm tra tính khả dụng của nhiều sản phẩm - Public
        /// </summary>
        [HttpPost("check-availability")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Kiểm tra tính khả dụng của nhiều sản phẩm",
            Description = "Dùng khi checkout để validate số lượng tồn kho."
        )]
        public async Task<IActionResult> CheckAvailability(string productId, int quantity)
        {
            var command = new CheckProductsAvailabilityQuery(productId, quantity);
            var result = await _mediator.Send(command);

            return Ok(result);
        }

        // ==================== COMMAND ENDPOINTS ====================

        /// <summary>
        /// Tạo hoặc cập nhật inventory item - Admin only
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Tạo hoặc cập nhật inventory item",
            Description = "Admin tạo mới hoặc cập nhật thông tin tồn kho cho sản phẩm."
        )]
        public async Task<IActionResult> CreateOrUpdate(
            [FromBody] CreateOrUpdateInventoryItemCommand command)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            _logger.LogInformation(
                "[CreateOrUpdateInventory] Admin {AdminName} creating/updating inventory for product {ProductId}",
                adminName, command.ProductId);

            var result = await _mediator.Send(command);

            return Ok(new
            {
                Message = "Inventory item created or updated successfully",
                ProductId = result.ProductId,
                Quantity = result.Quantity,
                Status = result.Status
            });
        }

        /// <summary>
        /// Tăng số lượng tồn kho - Seller/Admin
        /// </summary>
        [HttpPatch("{productId}/increase")]
        [Authorize(Policy = "SellerOrAdmin")]
        [SwaggerOperation(
            Summary = "Tăng số lượng tồn kho",
            Description = "Seller tăng tồn kho cho sản phẩm của mình, Admin có thể tăng cho bất kỳ sản phẩm nào."
        )]
        public async Task<IActionResult> IncreaseStock(
            string productId,
            [FromBody] UpdateStockRequest request)
        {
            var userId = HttpContext.GetUserId();
            var userName = HttpContext.GetName();
            var userRole = HttpContext.GetRole();

            if (request.Quantity <= 0)
                return BadRequest(new { Message = "Quantity must be greater than 0" });

            _logger.LogInformation(
                "[IncreaseStock] User {UserName} increasing stock for {ProductId} by {Quantity}",
                userName, productId, request.Quantity);

            var command = new IncreaseStockCommand(
                productId,
                request.Quantity,
                userId,
                userRole,
                request.Note);

            try
            {
                var result = await _mediator.Send(command);

                return Ok(new
                {
                    Message = $"Increased stock for {productId} by {request.Quantity}",
                    ProductId = result.ProductId,
                    NewQuantity = result.Quantity,
                    UpdatedBy = userName
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Giảm số lượng tồn kho - Seller/Admin
        /// </summary>
        [HttpPatch("{productId}/decrease")]
        [Authorize(Policy = "SellerOrAdmin")]
        [SwaggerOperation(
            Summary = "Giảm số lượng tồn kho",
            Description = "Seller giảm tồn kho cho sản phẩm của mình, Admin có thể giảm cho bất kỳ sản phẩm nào."
        )]
        public async Task<IActionResult> DecreaseStock(
            string productId,
            [FromBody] UpdateStockRequest request)
        {
            var userId = HttpContext.GetUserId();
            var userName = HttpContext.GetName();
            var userRole = HttpContext.GetRole();

            if (request.Quantity <= 0)
                return BadRequest(new { Message = "Quantity must be greater than 0" });

            _logger.LogInformation(
                "[DecreaseStock] User {UserName} decreasing stock for {ProductId} by {Quantity}",
                userName, productId, request.Quantity);

            var command = new DecreaseStockCommand(
                productId,
                request.Quantity,
                userId,
                userRole,
                request.Note);

            try
            {
                var result = await _mediator.Send(command);

                return Ok(new
                {
                    Message = $"Decreased stock for {productId} by {request.Quantity}",
                    ProductId = result.ProductId,
                    NewQuantity = result.Quantity,
                    UpdatedBy = userName
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
        }

        /// <summary>
        /// Reserve stock - Internal endpoint (được gọi từ Order Service)
        /// </summary>
        [HttpPost("{productId}/reserve")]
        [Authorize(Policy = "ServiceToService")] // Chỉ service-to-service calls
        [SwaggerOperation(
            Summary = "Reserve stock khi tạo order",
            Description = "Internal endpoint - được gọi từ Order Service để reserve tồn kho."
        )]
        public async Task<IActionResult> ReserveStock(
            string productId,
            [FromBody] ReserveStockRequest request)
        {
            _logger.LogInformation(
                "[ReserveStock] Reserving {Quantity} units of {ProductId} for order {OrderId}",
                request.Quantity, productId, request.OrderId);

            var command = new ReserveStockCommand(
                productId,
                request.Quantity,
                request.OrderId);

            try
            {
                var result = await _mediator.Send(command);

                return Ok(new
                {
                    Message = "Stock reserved successfully",
                    ProductId = result.ProductId,
                    ReservedQuantity = request.Quantity,
                    AvailableQuantity = result.AvailableQuantity
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Release reserved stock - Internal endpoint
        /// </summary>
        [HttpPost("{productId}/release")]
        [Authorize(Policy = "ServiceToService")]
        [SwaggerOperation(
            Summary = "Release reserved stock khi order bị hủy",
            Description = "Internal endpoint - được gọi khi order bị cancelled/failed."
        )]
        public async Task<IActionResult> ReleaseStock(
            string productId,
            [FromBody] ReleaseStockRequest request)
        {
            _logger.LogInformation(
                "[ReleaseStock] Releasing {Quantity} units of {ProductId} for order {OrderId}",
                request.Quantity, productId, request.OrderId);

            var command = new ReleaseStockCommand(
                productId,
                request.Quantity,
                request.OrderId);

            var result = await _mediator.Send(command);

            return Ok(new
            {
                Message = "Stock released successfully",
                ProductId = result.ProductId,
                AvailableQuantity = result.AvailableQuantity
            });
        }

        /// <summary>
        /// Confirm stock deduction - Internal endpoint
        /// </summary>
        [HttpPost("{productId}/confirm-deduction")]
        [Authorize(Policy = "ServiceToService")]
        [SwaggerOperation(
            Summary = "Confirm stock deduction khi order completed",
            Description = "Internal endpoint - chuyển reserved stock thành actual deduction."
        )]
        public async Task<IActionResult> ConfirmStockDeduction(
            string productId,
            [FromBody] ConfirmDeductionRequest request)
        {
            _logger.LogInformation(
                "[ConfirmDeduction] Confirming deduction of {Quantity} units of {ProductId} for order {OrderId}",
                request.Quantity, productId, request.OrderId);

            var command = new ConfirmStockDeductionCommand(
                productId,
                request.Quantity,
                request.OrderId);

            var result = await _mediator.Send(command);

            return Ok(new
            {
                Message = "Stock deduction confirmed",
                ProductId = result.ProductId,
                NewQuantity = result.Quantity
            });
        }

        /// <summary>
        /// Đặt ngưỡng cảnh báo sắp hết hàng - Seller/Admin
        /// </summary>
        [HttpPatch("{productId}/low-stock-threshold")]
        [Authorize(Policy = "SellerOrAdmin")]
        [SwaggerOperation(
            Summary = "Đặt ngưỡng cảnh báo sắp hết hàng",
            Description = "Seller/Admin đặt mức tồn kho tối thiểu để nhận cảnh báo."
        )]
        public async Task<IActionResult> SetLowStockThreshold(
            string productId,
            [FromBody] SetThresholdRequest request)
        {
            var userId = HttpContext.GetUserId();
            var userRole = HttpContext.GetRole();

            var command = new SetLowStockThresholdCommand(
                productId,
                request.Threshold,
                userId,
                userRole);

            try
            {
                var result = await _mediator.Send(command);

                return Ok(new
                {
                    Message = "Low stock threshold updated",
                    ProductId = result.ProductId,
                    Threshold = result.LowStockThreshold
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        // ==================== ADMIN ENDPOINTS ====================

        /// <summary>
        /// Lấy lịch sử thay đổi inventory - Admin only
        /// </summary>
        [HttpGet("{productId}/history")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Lấy lịch sử thay đổi tồn kho",
            Description = "Admin xem tất cả thay đổi tồn kho của sản phẩm."
        )]
        public async Task<IActionResult> GetInventoryHistory(
            string productId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = new GetInventoryHistoryQuery(
                productId, fromDate, toDate, page, pageSize);

            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Lấy thống kê inventory - Admin only
        /// </summary>
        [HttpGet("admin/statistics")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Lấy thống kê tồn kho",
            Description = "Thống kê tổng quan về tồn kho trong hệ thống."
        )]
        public async Task<IActionResult> GetInventoryStatistics()
        {
            var query = new GetInventoryStatisticsQuery();
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Bulk update inventory - Admin only
        /// </summary>
        [HttpPost("admin/bulk-update")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Cập nhật hàng loạt tồn kho",
            Description = "Admin cập nhật nhiều sản phẩm cùng lúc."
        )]
        public async Task<IActionResult> BulkUpdateInventory(
            [FromBody] BulkUpdateInventoryCommand command)
        {
            var adminName = HttpContext.GetName();

            _logger.LogInformation(
                "[BulkUpdate] Admin {AdminName} bulk updating {Count} products",
                adminName, command.Items.Count);

            var result = await _mediator.Send(command);

            return Ok(new
            {
                Message = "Bulk update completed",
                SuccessCount = result.SuccessCount,
                FailureCount = result.FailureCount,
                Errors = result.Errors
            });
        }
    }

    // ==================== DTOs ====================

    public class UpdateStockRequest
    {
        public int Quantity { get; set; }
        public string? Note { get; set; }
    }

    public class ReserveStockRequest
    {
        public int Quantity { get; set; }
        public Guid OrderId { get; set; }
    }

    public class ReleaseStockRequest
    {
        public int Quantity { get; set; }
        public Guid OrderId { get; set; }
    }

    public class ConfirmDeductionRequest
    {
        public int Quantity { get; set; }
        public Guid OrderId { get; set; }
    }

    public class SetThresholdRequest
    {
        public int Threshold { get; set; }
    }


}
