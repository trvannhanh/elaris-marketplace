using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.OrderService.API.Extensions;
using Services.OrderService.Application.Orders.Commands.CancelOrder;
using Services.OrderService.Application.Orders.Commands.ChangeStatus;
using Services.OrderService.Application.Orders.Commands.CreateOrder;
using Services.OrderService.Application.Orders.DTOs;
using Services.OrderService.Application.Orders.GetOrdersWithFilters;
using Services.OrderService.Application.Orders.Queries.GetMyOrders;
using Services.OrderService.Application.Orders.Queries.GetOrderById;
using Services.OrderService.Application.Orders.Queries.GetOrderStatistics;
using Services.OrderService.Domain.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace Services.OrderService.API.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // ==================== PUBLIC/BUYER/SELLER ENDPOINTS ====================

        /// <summary>
        /// Tạo order mới - Buyer và Seller đều được tạo
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "BuyerOrSeller")]
        [SwaggerOperation(
            Summary = "Tạo order mới",
            Description = "Buyer hoặc Seller có thể tạo order ngay khi cần Đặt hàng ngay với 1 sản phẩm không phải từ giỏ hàng , bao gồm thông tin item và total price."
        )]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            // Lấy userId từ token, KHÔNG tin client
            var userId = HttpContext.GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token");

            _logger.LogInformation("[CreateOrder] User ({UserId}) creating order", userId);

            // Map request DTO to Command
            var command = new CreateOrderCommand(
                UserId: userId,
                ProductId: request.ProductId,
                Quantity: request.Quantity,
                CardToken: request.CardToken
            );

            var order = await _mediator.Send(command);

            return CreatedAtAction(
                nameof(GetOrderById),
                new { orderId = order.Id },
                new
                {
                    Message = "Order created successfully",
                    OrderId = order.Id,
                    Status = order.Status.ToString(),
                    TotalPrice = order.TotalPrice,
                    CreatedAt = order.CreatedAt
                }
            );
        }

        /// <summary>
        /// Lấy chi tiết order - User chỉ xem order của mình, Admin xem tất cả
        /// </summary>
        [HttpGet("{orderId:guid}")]
        [Authorize(Policy = "Authenticated")]
        [SwaggerOperation(
            Summary = "Lấy chi tiết order",
            Description = "User chỉ xem order của mình, Admin xem tất cả."
        )]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var userId = HttpContext.GetUserId();

            var userRole = HttpContext.GetRole();


            var order = await _mediator.Send(new GetOrderByIdQuery(orderId));

            if (order == null)
                return NotFound(new { Message = $"Order {orderId} not found" });

            // Admin xem được tất cả
            if (userRole == "admin")
            {
                _logger.LogInformation("[GetOrderById] Admin viewing order {OrderId}", orderId);
                return Ok(order);
            }

            // User chỉ xem order của mình
            if (order.UserId != userId)
            {
                _logger.LogWarning("[GetOrderById] User {UserId} attempted to view order {OrderId} owned by {OwnerId}",
                    userId, orderId, order.UserId);
                return Forbid();
            }

            return Ok(order);
        }

        /// <summary>
        /// Lấy danh sách orders của user hiện tại - Buyer/Seller
        /// </summary>
        [HttpGet("my-orders")]
        [Authorize(Policy = "BuyerOrSeller")]
        [SwaggerOperation(
            Summary = "Lấy danh sách orders của user",
            Description = "Buyer/Seller xem danh sách Order của mình"
        )]
        public async Task<IActionResult> GetMyOrders(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = HttpContext.GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("userId is null or empty");

            var query = new GetMyOrdersQuery(userId, page, pageSize);
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Hủy order - User chỉ hủy order của mình (khi Pending)
        /// </summary>
        [HttpPost("{orderId:guid}/cancel")]
        [Authorize(Policy = "BuyerOrSeller")]
        [SwaggerOperation(
            Summary = "Hủy đơn hàng",
            Description = "Buyer/Seller hủy order - User chỉ hủy order của mình (khi Pending)"
        )]
        public async Task<IActionResult> CancelOrder(Guid orderId, [FromBody] CancelOrderRequest? request)
        {
            var userId = HttpContext.GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var command = new CancelOrderCommand(
                OrderId: orderId,
                UserId: userId,
                Reason: request?.Reason
            );

            try
            {
                var success = await _mediator.Send(command);

                if (success)
                {
                    return Ok(new { Message = "Order cancelled successfully" });
                }

                return BadRequest(new { Message = "Failed to cancel order" });
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
                return NotFound(new { Message = ex.Message });
            }
        }

        // ==================== ADMIN ONLY ENDPOINTS ====================

        /// <summary>
        /// Lấy tất cả orders với filters - ADMIN ONLY
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Lấy tất cả orders với filters",
            Description = "Chỉ Admin có thể lấy tất cả orders với filters"
        )]
        public async Task<IActionResult> GetAllOrders(
            [FromQuery] string? search,
            [FromQuery] string? userId,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortDirection,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetOrdersWithFiltersQuery(
                search, userId, sortBy, sortDirection, page, pageSize);

            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Update order status - ADMIN ONLY
        /// </summary>
        [HttpPut("{orderId:guid}/status")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Cập nhật trạng thái đơn hàng.",
            Description = "Chỉ Admin có thể Cập nhật trạng thái đơn hàng"
        )]
        public async Task<IActionResult> UpdateOrderStatus(
            Guid orderId,
            [FromBody] UpdateOrderStatusRequest request)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            _logger.LogInformation("[UpdateOrderStatus] Admin {AdminName} updating order {OrderId} to status {Status}",
                adminName, orderId, request.Status);

            var command = new UpdateOrderStatusCommand(
                OrderId: orderId,
                NewStatus: request.Status,
                Note: request.Note
            );

            try
            {
                var success = await _mediator.Send(command);

                if (success)
                {
                    return Ok(new
                    {
                        Message = "Order status updated successfully",
                        OrderId = orderId,
                        NewStatus = request.Status.ToString(),
                        UpdatedBy = adminName
                    });
                }

                return BadRequest(new { Message = "Failed to update order status" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Force cancel order - ADMIN ONLY (bypass all checks)
        /// </summary>
        [HttpPost("{orderId:guid}/admin/cancel")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Hủy hàng loạt đơn hàng.",
            Description = "Chỉ Admin có thể hủy hàng loạt đơn hàng"
        )]
        public async Task<IActionResult> AdminCancelOrder(
            Guid orderId,
            [FromBody] AdminCancelOrderRequest request)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();


            _logger.LogInformation("[AdminCancelOrder] Admin {AdminName} force cancelling order {OrderId}",
                adminName, orderId);

            // Admin force cancel (không check ownership, không check status)
            var command = new UpdateOrderStatusCommand(
                OrderId: orderId,
                NewStatus: OrderStatus.Cancelled,
                Note: $"Cancelled by admin: {request.Reason}"
            );

            try
            {
                await _mediator.Send(command);

                return Ok(new
                {
                    Message = "Order cancelled by admin",
                    OrderId = orderId,
                    CancelledBy = adminName,
                    Reason = request.Reason
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Lấy thống kê orders - ADMIN ONLY
        /// </summary>
        [HttpGet("admin/statistics")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Lấy thống kê đơn hàng.",
            Description = "Chỉ Admin có thể lấy thống kê tất cả đơn hàng"
        )]
        public async Task<IActionResult> GetOrderStatistics(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var query = new GetOrderStatisticsQuery(fromDate, toDate);
            var result = await _mediator.Send(query);

            return Ok(result);
        }

        /// <summary>
        /// Lấy orders theo status - ADMIN ONLY
        /// </summary>
        [HttpGet("admin/by-status/{status}")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Lấy đơn hàng theo trạng thái.",
            Description = "Chỉ Admin có thể lấy đơn hàng theo trạng thái" +
            " Pending : 0,\r\nProcessing : 1,\r\nCompleted : 2,\r\nFailed : 3,\r\nCancelled : 4"
        )]
        public async Task<IActionResult> GetOrdersByStatus(
            OrderStatus status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Reuse GetOrdersWithFiltersQuery nhưng filter theo status
            // Có thể tạo query riêng nếu cần logic phức tạp hơn

            var query = new GetOrdersWithFiltersQuery(
                Search: null,
                UserId: null,
                SortBy: "createdAt",
                SortDirection: "desc",
                Page: page,
                PageSize: pageSize
            );

            var result = await _mediator.Send(query);

            // Filter by status (hoặc thêm vào query handler)
            var filteredItems = result.Items.Where(o => o.Status == status.ToString());

            return Ok(new
            {
                Items = filteredItems,
                result.PageNumber,
                result.PageSize,
                TotalCount = filteredItems.Count()
            });
        }
    }
}
