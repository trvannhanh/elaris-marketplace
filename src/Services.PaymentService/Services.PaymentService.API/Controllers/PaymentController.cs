
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.PaymentService.API.Extensions;
using Services.PaymentService.Application.DTOs;
using Services.PaymentService.Application.Payments.Commands.CancelPayment;
using Services.PaymentService.Application.Payments.Commands.CapturePayment;
using Services.PaymentService.Application.Payments.Commands.CreatePayment;
using Services.PaymentService.Application.Payments.Commands.ProcessPayment;
using Services.PaymentService.Application.Payments.Commands.RefundPayment;
using Services.PaymentService.Application.Payments.Commands.RetryPayment;
using Services.PaymentService.Application.Payments.Commands.UpdatePaymentStatus;
using Services.PaymentService.Application.Payments.Queries.GetAllPayments;
using Services.PaymentService.Application.Payments.Queries.GetFailedPayments;
using Services.PaymentService.Application.Payments.Queries.GetPaymentById;
using Services.PaymentService.Application.Payments.Queries.GetPaymentByOrderId;
using Services.PaymentService.Application.Payments.Queries.GetPaymentReconciliation;
using Services.PaymentService.Application.Payments.Queries.GetPaymentStatistics;
using Services.PaymentService.Application.Payments.Queries.GetUserPayments;
using Services.PaymentService.Domain.Entities;
using Swashbuckle.AspNetCore.Annotations;

namespace Services.PaymentService.API.Controllers
{
    /// <summary>
    /// Controller quản lý thanh toán với phân quyền JWT
    /// </summary>
    [ApiController]
    [Route("api/payments")]
    public class PaymentController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IMediator mediator, ILogger<PaymentController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        // ==================== QUERY ENDPOINTS ====================

        /// <summary>
        /// Lấy thông tin payment theo ID - User và Admin
        /// </summary>
        [HttpGet("{id:guid}")]
        [Authorize(Policy = "UserOrAdmin")]
        [SwaggerOperation(
            Summary = "Lấy thông tin payment theo ID",
            Description = "User chỉ xem payment của mình, Admin xem tất cả."
        )]
        public async Task<IActionResult> GetPayment(Guid id, CancellationToken ct)
        {
            var userId = HttpContext.GetUserId();
            var userName = HttpContext.GetName();
            var isAdmin = HttpContext.GetRole() == "admin";

            _logger.LogInformation(
                "[GetPayment] User {UserName} retrieving payment {PaymentId}",
                userName, id);

            var query = new GetPaymentByIdQuery(id, userId, isAdmin);

            try
            {
                var payment = await _mediator.Send(query, ct);

                return Ok(payment);
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
        /// Lấy payment theo OrderId - User và Admin
        /// </summary>
        [HttpGet("order/{orderId:guid}")]
        [Authorize(Policy = "UserOrAdmin")]
        [SwaggerOperation(
            Summary = "Lấy payment theo OrderId",
            Description = "User chỉ xem payment của order mình, Admin xem tất cả."
        )]
        public async Task<IActionResult> GetPaymentByOrderId(Guid orderId, CancellationToken ct)
        {
            var userId = HttpContext.GetUserId();
            var userName = HttpContext.GetName();
            var isAdmin = HttpContext.GetRole() == "admin";

            _logger.LogInformation(
                "[GetPaymentByOrderId] User {UserName} retrieving payment for order {OrderId}",
                userName, orderId);

            var query = new GetPaymentByOrderIdQuery(orderId, userId, isAdmin);

            try
            {
                var payment = await _mediator.Send(query, ct);

                return Ok(payment);
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
        /// Lấy danh sách payments của user - User và Admin
        /// </summary>
        [HttpGet("my-payments")]
        [Authorize(Policy = "UserOrAdmin")]
        [SwaggerOperation(
            Summary = "Lấy danh sách payments của user",
            Description = "User xem payments của mình, Admin có thể xem tất cả với userId param."
        )]
        public async Task<IActionResult> GetMyPayments(
            [FromQuery] string? userId,
            [FromQuery] PaymentStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var currentUserId = HttpContext.GetUserId();
            var userName = HttpContext.GetName();
            var isAdmin = HttpContext.GetRole() == "admin";

            // Nếu không phải admin, chỉ xem payment của mình
            if (!isAdmin)
            {
                userId = currentUserId;
            }
            else if (string.IsNullOrEmpty(userId))
            {
                userId = currentUserId;
            }

            _logger.LogInformation(
                "[GetMyPayments] User {UserName} retrieving payments for user {UserId}",
                userName, userId);

            var query = new GetUserPaymentsQuery(userId, status, page, pageSize);
            var result = await _mediator.Send(query, ct);

            return Ok(result);
        }

        /// <summary>
        /// Lấy payment statistics - User và Admin
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Policy = "UserOrAdmin")]
        [SwaggerOperation(
            Summary = "Lấy thống kê thanh toán",
            Description = "User xem thống kê của mình, Admin xem thống kê tổng thể."
        )]
        public async Task<IActionResult> GetPaymentStatistics(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            CancellationToken ct = default)
        {
            var userId = HttpContext.GetUserId();
            var isAdmin = HttpContext.GetRole() == "admin";

            var query = new GetPaymentStatisticsQuery(
                isAdmin ? null : userId,
                fromDate,
                toDate);

            var result = await _mediator.Send(query, ct);

            return Ok(result);
        }

        // ==================== COMMAND ENDPOINTS ====================

        /// <summary>
        /// Tạo payment mới - User và Admin
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "UserOrAdmin")]
        [SwaggerOperation(
            Summary = "Tạo payment mới",
            Description = "Tạo payment pending cho order, chờ xử lý thanh toán."
        )]
        public async Task<IActionResult> CreatePayment(
            [FromBody] CreatePaymentCommand cmd,
            CancellationToken ct)
        {
            var userId = HttpContext.GetUserId();
            var userName = HttpContext.GetName();

            _logger.LogInformation(
                "[CreatePayment] User {UserName} (ID: {UserId}) creating payment for order {OrderId}",
                userName, userId, cmd.OrderId);

            try
            {
                var payment = await _mediator.Send(cmd, ct);

                return CreatedAtAction(
                    nameof(GetPayment),
                    new { id = payment.Id },
                    new
                    {
                        payment.Id,
                        payment.Status,
                        payment.OrderId,
                        payment.Amount,
                        CreatedBy = userName,
                        CreatedAt = payment.CreatedAt
                    });
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
        /// Process payment - Internal endpoint hoặc User
        /// </summary>
        [HttpPost("{id:guid}/process")]
        [Authorize(Policy = "UserOrAdmin")]
        [SwaggerOperation(
            Summary = "Xử lý thanh toán",
            Description = "User thực hiện thanh toán cho payment đang pending."
        )]
        public async Task<IActionResult> ProcessPayment(
            Guid id,
            [FromBody] ProcessPaymentRequest request,
            CancellationToken ct)
        {
            var userId = HttpContext.GetUserId();
            var userName = HttpContext.GetName();

            _logger.LogInformation(
                "[ProcessPayment] User {UserName} processing payment {PaymentId}",
                userName, id);

            var command = new ProcessPaymentCommand(
                id,
                request.PaymentDetails);

            try
            {
                var result = await _mediator.Send(command, ct);

                return Ok(new
                {
                    Message = "Payment processed successfully",
                    PaymentId = result.Id,
                    Status = result.Status,
                    TransactionId = result.TransactionId,
                    ProcessedAt = result.ProcessedAt
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
                return NotFound(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Retry failed payment - User
        /// </summary>
        [HttpPost("{id:guid}/retry")]
        [Authorize(Policy = "UserOrAdmin")]
        [SwaggerOperation(
            Summary = "Retry thanh toán thất bại",
            Description = "User thử lại thanh toán đã failed."
        )]
        public async Task<IActionResult> RetryPayment(
            Guid id,
            [FromBody] RetryPaymentRequest request,
            CancellationToken ct)
        {
            var userId = HttpContext.GetUserId();
            var userName = HttpContext.GetName();

            _logger.LogInformation(
                "[RetryPayment] User {UserName} retrying payment {PaymentId}",
                userName, id);

            var command = new RetryPaymentCommand(
                id,
                request.PaymentDetails);

            try
            {
                var result = await _mediator.Send(command, ct);

                return Ok(new
                {
                    Message = "Payment retry initiated",
                    PaymentId = result.Id,
                    Status = result.Status
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
        }

        /// <summary>
        /// Cancel payment - User và Admin
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        [Authorize(Policy = "UserOrAdmin")]
        [SwaggerOperation(
            Summary = "Hủy thanh toán",
            Description = "User hủy payment đang pending, Admin có thể hủy bất kỳ payment nào."
        )]
        public async Task<IActionResult> CancelPayment(
            Guid id,
            [FromBody] CancelPaymentRequest? request,
            CancellationToken ct)
        {
            var userId = HttpContext.GetUserId();
            var userName = HttpContext.GetName();

            _logger.LogInformation(
                "[CancelPayment] User {UserName} cancelling payment {PaymentId}",
                userName, id);

            var command = new CancelPaymentCommand(
                id,
                request?.Reason,
                userId);

            try
            {
                var result = await _mediator.Send(command, ct);

                return Ok(new
                {
                    Message = "Payment cancelled successfully",
                    PaymentId = result.Id,
                    Status = result.Status,
                    CancelledBy = userName
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
        }

        // ==================== ADMIN ENDPOINTS ====================

        /// <summary>
        /// Lấy tất cả payments với filters - Admin only
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Lấy tất cả payments với filters",
            Description = "Admin xem tất cả payments trong hệ thống với filters."
        )]
        public async Task<IActionResult> GetAllPayments(
            [FromQuery] string? search,
            [FromQuery] string? userId,
            [FromQuery] PaymentStatus? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var query = new GetAllPaymentsQuery(
                search, userId, status, fromDate, toDate, page, pageSize);

            var result = await _mediator.Send(query, ct);

            return Ok(result);
        }

        /// <summary>
        /// Capture payment - Admin only
        /// </summary>
        [HttpPost("{id:guid}/capture")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Capture payment đã pre-authorize",
            Description = "Admin thực sự charge tiền từ customer."
        )]
        public async Task<IActionResult> CapturePayment(Guid id, decimal amount, CancellationToken ct)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            _logger.LogInformation(
                "[CapturePayment] Admin {AdminName} capturing payment {PaymentId}",
                adminName, id);

            var command = new CapturePaymentCommand(id, amount, adminId);

            try
            {
                var result = await _mediator.Send(command, ct);

                return Ok(new
                {
                    Message = "Payment captured successfully",
                    PaymentId = result.Id,
                    Status = result.Status,
                    CapturedAmount = result.Amount,
                    CapturedBy = adminName,
                    CapturedAt = result.CapturedAt
                });
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
        /// Refund payment - Admin only
        /// </summary>
        [HttpPost("{id:guid}/refund")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Hoàn tiền cho customer",
            Description = "Admin thực hiện refund toàn bộ hoặc một phần."
        )]
        public async Task<IActionResult> RefundPayment(
            Guid id,
            [FromBody] RefundPaymentRequest request,
            CancellationToken ct)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            _logger.LogInformation(
                "[RefundPayment] Admin {AdminName} refunding payment {PaymentId}. Amount: {Amount}, Reason: {Reason}",
                adminName, id, request.Amount, request.Reason);

            var command = new RefundPaymentCommand(
                id,
                request.Amount,
                request.Reason,
                adminId
                );

            try
            {
                var result = await _mediator.Send(command, ct);

                return Ok(new
                {
                    Message = "Payment refunded successfully",
                    PaymentId = result.Id,
                    RefundedAmount = result.RefundedAmount,
                    Status = result.Status,
                    Reason = request.Reason,
                    RefundedBy = adminName,
                    RefundedAt = result.RefundedAt
                });
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
        /// Update payment status - Admin only
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Cập nhật trạng thái payment",
            Description = "Admin thay đổi trạng thái payment (dùng cho manual processing)."
        )]
        public async Task<IActionResult> UpdatePaymentStatus(
            Guid id,
            [FromBody] UpdatePaymentStatusRequest request,
            CancellationToken ct)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            _logger.LogInformation(
                "[UpdatePaymentStatus] Admin {AdminName} updating payment {PaymentId} to status {Status}",
                adminName, id, request.Status);

            var command = new UpdatePaymentStatusCommand(
                id,
                request.Status,
                request.Note,
                adminId);

            try
            {
                var result = await _mediator.Send(command, ct);

                return Ok(new
                {
                    Message = "Payment status updated successfully",
                    PaymentId = result.Id,
                    NewStatus = result.Status,
                    UpdatedBy = adminName
                });
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
        /// Get payment reconciliation report - Admin only
        /// </summary>
        [HttpGet("admin/reconciliation")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Lấy báo cáo đối soát thanh toán",
            Description = "Admin xem báo cáo đối soát theo khoảng thời gian."
        )]
        public async Task<IActionResult> GetReconciliationReport(
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            CancellationToken ct)
        {
            var query = new GetPaymentReconciliationQuery(fromDate, toDate);
            var result = await _mediator.Send(query, ct);

            return Ok(result);
        }

        /// <summary>
        /// Get failed payments - Admin only
        /// </summary>
        [HttpGet("admin/failed")]
        [Authorize(Policy = "Admin")]
        [SwaggerOperation(
            Summary = "Lấy danh sách payments thất bại",
            Description = "Admin xem các payment failed để xử lý."
        )]
        public async Task<IActionResult> GetFailedPayments(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var query = new GetFailedPaymentsQuery(fromDate, toDate, page, pageSize);
            var result = await _mediator.Send(query, ct);

            return Ok(result);
        }

        //// ==================== WEBHOOK ENDPOINTS ====================

        ///// <summary>
        ///// Payment gateway webhook - Public endpoint
        ///// </summary>
        //[HttpPost("webhook/payment-gateway")]
        //[AllowAnonymous]
        //[SwaggerOperation(
        //    Summary = "Webhook từ payment gateway",
        //    Description = "Nhận callback từ payment gateway (VNPay, Stripe, etc.)."
        //)]
        //public async Task<IActionResult> PaymentGatewayWebhook(
        //    [FromBody] PaymentWebhookDto webhook,
        //    CancellationToken ct)
        //{
        //    _logger.LogInformation(
        //        "[PaymentWebhook] Received webhook for payment {PaymentId}",
        //        webhook.PaymentId);

        //    var command = new ProcessPaymentWebhookCommand(
        //        webhook.PaymentId,
        //        webhook.Status,
        //        webhook.TransactionId,
        //        webhook.Signature,
        //        webhook.Data);

        //    try
        //    {
        //        await _mediator.Send(command, ct);

        //        return Ok(new { Message = "Webhook processed successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "[PaymentWebhook] Failed to process webhook");
        //        return StatusCode(500, new { Message = "Webhook processing failed" });
        //    }
        //}

        // ==================== HEALTH & DEBUG ENDPOINTS ====================

        /// <summary>
        /// Health check - Public
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Health check endpoint",
            Description = "Kiểm tra service đang hoạt động."
        )]
        public IActionResult Health()
        {
            return Ok(new
            {
                Service = "PaymentService",
                Status = "Healthy",
                Timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get current user info - Debug endpoint
        /// </summary>
        [HttpGet("me")]
        [Authorize(Policy = "ApiAccess")]
        [SwaggerOperation(
            Summary = "Lấy thông tin user hiện tại",
            Description = "Debug endpoint - xem claims trong JWT token."
        )]
        public IActionResult GetCurrentUser()
        {
            var claims = User.Claims.Select(c => new
            {
                Type = c.Type,
                Value = c.Value
            });

            return Ok(new
            {
                UserId = HttpContext.GetUserId(),
                UserName = HttpContext.GetName(),
                Email = User.FindFirst("email")?.Value,
                Role = HttpContext.GetRole(),
                AllClaims = claims
            });
        }
    }



    // ==================== DTOs ====================
   
}