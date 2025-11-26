using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.IdentityService.Data;
using Services.IdentityService.Data.Entities;
using Services.IdentityService.DTOs;
using Services.IdentityService.Extensions;
using Swashbuckle.AspNetCore.Annotations;

namespace Services.IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<UserController> _logger;

        public UserController(
            UserManager<AppUser> userManager,
            AppDbContext dbContext,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger;
        }

        // ==================== USER PROFILE ENDPOINTS ====================

        /// <summary>
        /// Lấy profile của user hiện tại
        /// </summary>
        [SwaggerOperation(
            Summary = "Lấy profile của user hiện tại",
            Description = "Trả về thông tin tài khoản của người dùng đang đăng nhập."
        )]
        [HttpGet("me")]
        [Authorize(Policy = "Authenticated")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = HttpContext.GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            return Ok(new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email,
                DisplayName = user.DisplayName,
                Bio = user.Bio,
                Avatar = user.Avatar,
                Role = role,
                CreatedAt = user.CreatedAt,
                IsVerified = user.IsVerified,
                IsBanned = user.IsBanned
            });
        }

        /// <summary>
        /// Cập nhật profile
        /// </summary>
        [SwaggerOperation(
            Summary = "Cập nhật profile",
            Description = "Cho phép người dùng thay đổi DisplayName, Bio, Avatar, PhoneNumber."
        )]
        [HttpPut("me")]
        [Authorize(Policy = "Authenticated")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = HttpContext.GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            // Update fields
            if (dto.DisplayName != null) user.DisplayName = dto.DisplayName;
            if (dto.Bio != null) user.Bio = dto.Bio;
            if (dto.Avatar != null) user.Avatar = dto.Avatar;
            if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            return Ok(new { Message = "Profile updated successfully" });
        }

        /// <summary>
        /// Đổi password
        /// </summary>
        [SwaggerOperation(
            Summary = "Đổi password",
            Description = "Người dùng phải nhập đúng mật khẩu hiện tại để đổi sang mật khẩu mới."
        )]
        [HttpPost("me/change-password")]
        [Authorize(Policy = "Authenticated")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var userId = HttpContext.GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new { Message = "Failed to change password", Errors = result.Errors });

            return Ok(new { Message = "Password changed successfully" });
        }

        [SwaggerOperation(
            Summary = "Trở thành người bán (Seller)",
            Description = "Gán role 'seller' cho user hiện tại. Nếu user đã có role seller thì trả lỗi. ** Sau khi trở thành Seller cần refresh lại token để access token có role seller"
        )]
        [HttpPost("me/become-seller")]
        [Authorize(Policy = "Authenticated")]
        public async Task<IActionResult> BecomeSeller()
        {
            var userId = HttpContext.GetUserId();
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("seller"))
                return BadRequest(new { Message = "Already a seller" });

            await _userManager.AddToRoleAsync(user, "seller");

            return Ok(new { Message = "You are now a seller" });
        }

        // ==================== SELLER PUBLIC PROFILE ====================

        /// <summary>
        /// Xem public profile của seller (ai cũng xem được)
        /// </summary>
        [SwaggerOperation(
            Summary = "Xem public profile của seller",
            Description = "Bất kỳ ai cũng có thể xem. Nếu user không phải seller sẽ trả lỗi."
        )]
        [HttpGet("sellers/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSellerProfile(string id)
        {
            var seller = await _userManager.FindByIdAsync(id);

            if (seller == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(seller);

            if (!roles.Contains("seller"))
                return BadRequest(new { Message = "User is not a seller" });

            return Ok(new SellerProfileDto
            {
                Id = seller.Id,
                UserName = seller.UserName ?? "",
                DisplayName = seller.DisplayName,
                Bio = seller.Bio,
                Avatar = seller.Avatar,
                Rating = seller.Rating,
                RatingCount = seller.RatingCount,
                TotalProducts = seller.TotalProducts,
                TotalSales = seller.TotalSales,
                CreatedAt = seller.CreatedAt,
                IsVerified = seller.IsVerified,
                VerifiedAt = seller.VerifiedAt,
                BusinessName = seller.BusinessName
            });
        }

        // ==================== SELLER VERIFICATION ENDPOINTS ====================

        /// <summary>
        /// Seller request verification
        /// </summary>
        [SwaggerOperation(
            Summary = "Seller gửi yêu cầu xác minh",
            Description = "Upload giấy phép kinh doanh, CMND/CCCD và thông tin doanh nghiệp."
        )]
        [HttpPost("me/request-verification")]
        [Authorize(Policy = "Seller")]
        public async Task<IActionResult> RequestVerification([FromForm] VerificationRequestDto dto)
        {
            var sellerId = HttpContext.GetUserId();

            if (string.IsNullOrEmpty(sellerId))
                return Unauthorized();

            var seller = await _userManager.FindByIdAsync(sellerId);

            if (seller == null)
                return NotFound();

            // Check if already verified
            if (seller.IsVerified)
                return BadRequest(new { Message = "You are already verified" });

            // Check if already has pending request
            if (seller.VerificationRequested && seller.VerificationStatus == "Pending")
                return BadRequest(new { Message = "You already have a pending verification request" });

            // TODO: Upload files to storage (Azure Blob, AWS S3, etc.)
            string? businessLicenseUrl = null;
            string? idCardUrl = null;

            if (dto.BusinessLicense != null)
            {
                // Upload file logic here
                businessLicenseUrl = $"/uploads/business-licenses/{Guid.NewGuid()}{Path.GetExtension(dto.BusinessLicense.FileName)}";
            }

            if (dto.IdCard != null)
            {
                // Upload file logic here
                idCardUrl = $"/uploads/id-cards/{Guid.NewGuid()}{Path.GetExtension(dto.IdCard.FileName)}";
            }

            // Update seller info
            seller.BusinessName = dto.BusinessName;
            seller.TaxId = dto.TaxId;
            seller.BusinessAddress = dto.BusinessAddress;
            seller.BusinessLicenseUrl = businessLicenseUrl;
            seller.IdCardUrl = idCardUrl;
            seller.VerificationRequested = true;
            seller.VerificationRequestedAt = DateTime.UtcNow;
            seller.VerificationStatus = "Pending";

            await _userManager.UpdateAsync(seller);

            _logger.LogInformation("[RequestVerification] Seller {SellerId} requested verification", sellerId);

            return Ok(new
            {
                Message = "Verification request submitted successfully. Admin will review within 3-5 business days.",
                RequestedAt = seller.VerificationRequestedAt
            });
        }


        // ==================== SELLER PAYOUT ENDPOINTS ====================

        /// <summary>
        /// Lấy thông tin payout của seller
        /// </summary>
        [SwaggerOperation(
            Summary = "Lấy thông tin payout của seller",
            Description = "Bao gồm số dư, thu nhập, tài khoản ngân hàng và lần rút gần nhất."
        )]
        [HttpGet("me/payout")]
        [Authorize(Policy = "Seller")]
        public async Task<IActionResult> GetPayoutInfo()
        {
            var sellerId = HttpContext.GetUserId();

            if (string.IsNullOrEmpty(sellerId))
                return Unauthorized();

            var seller = await _userManager.FindByIdAsync(sellerId);

            if (seller == null)
                return NotFound();

            // Get last payout
            var lastPayout = await _dbContext.PayoutRequests
                .Where(p => p.SellerId == sellerId && p.Status == PayoutStatus.Completed)
                .OrderByDescending(p => p.ProcessedAt)
                .FirstOrDefaultAsync();

            return Ok(new PayoutInfoDto
            {
                TotalEarnings = seller.TotalEarnings,
                Balance = seller.Balance,
                AvailableForPayout = seller.Balance, // Can add minimum threshold logic here
                BankAccount = seller.BankAccount,
                BankName = seller.BankName,
                BankAccountHolder = seller.BankAccountHolder,
                LastPayoutDate = lastPayout?.ProcessedAt
            });
        }

        /// <summary>
        /// Seller request payout
        /// </summary>
        [SwaggerOperation(
            Summary = "Yêu cầu rút tiền",
            Description = "Số tiền tối thiểu: 50.000 VND. Không được phép nếu đang có yêu cầu pending."
        )]
        [HttpPost("me/payout/request")]
        [Authorize(Policy = "Seller")]
        public async Task<IActionResult> RequestPayout([FromBody] CreatePayoutRequestDto dto)
        {
            var sellerId = HttpContext.GetUserId();
            var sellerName = HttpContext.GetName();

            if (string.IsNullOrEmpty(sellerId))
                return Unauthorized();

            var seller = await _userManager.FindByIdAsync(sellerId);

            if (seller == null)
                return NotFound();

            // Validate amount
            const decimal MinimumPayout = 50000; // 50k VND

            if (dto.Amount < MinimumPayout)
                return BadRequest(new { Message = $"Minimum payout amount is {MinimumPayout:N0} VND" });

            if (dto.Amount > seller.Balance)
                return BadRequest(new { Message = "Insufficient balance" });

            // Check for pending payout
            var hasPendingPayout = await _dbContext.PayoutRequests
                .AnyAsync(p => p.SellerId == sellerId && p.Status == PayoutStatus.Pending);

            if (hasPendingPayout)
                return BadRequest(new { Message = "You already have a pending payout request" });

            // Create payout request
            var payoutRequest = new PayoutRequest
            {
                SellerId = sellerId,
                Amount = dto.Amount,
                BankAccount = dto.BankAccount,
                BankName = dto.BankName,
                BankAccountHolder = dto.BankAccountHolder,
                Status = PayoutStatus.Pending
            };

            _dbContext.PayoutRequests.Add(payoutRequest);

            // Deduct from balance (reserve)
            seller.Balance -= dto.Amount;
            await _userManager.UpdateAsync(seller);

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("[RequestPayout] Seller {SellerName} ({SellerId}) requested payout of {Amount}",
                sellerName, sellerId, dto.Amount);

            return Ok(new
            {
                Message = "Payout request submitted successfully. Processing within 3-5 business days.",
                PayoutId = payoutRequest.Id,
                Amount = payoutRequest.Amount,
                Status = payoutRequest.Status.ToString()
            });
        }

        /// <summary>
        /// Lấy lịch sử payout của seller
        /// </summary>
        [SwaggerOperation(
            Summary = "Lấy lịch sử rút tiền",
            Description = "Trả về tất cả yêu cầu rút tiền của seller đang đăng nhập."
        )]
        [HttpGet("me/payout/history")]
        [Authorize(Policy = "Seller")]
        public async Task<IActionResult> GetPayoutHistory()
        {
            var sellerId = HttpContext.GetUserId();

            if (string.IsNullOrEmpty(sellerId))
                return Unauthorized();

            var payouts = await _dbContext.PayoutRequests
                .Where(p => p.SellerId == sellerId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PayoutRequestDto
                {
                    Id = p.Id,
                    SellerId = p.SellerId,
                    Amount = p.Amount,
                    BankAccount = p.BankAccount,
                    BankName = p.BankName,
                    Status = p.Status.ToString(),
                    CreatedAt = p.CreatedAt,
                    ProcessedAt = p.ProcessedAt,
                    ProcessedBy = p.ProcessedBy
                })
                .ToListAsync();

            return Ok(payouts);
        }

        // ==================== ADMIN ENDPOINTS ====================

        /// <summary>
        /// Admin: Lấy danh sách tất cả users
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Lấy danh sách users",
            Description = "Hỗ trợ phân trang & tìm kiếm theo username, email, displayName."
        )]
        [HttpGet("admin/all")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            var query = _userManager.Users.AsQueryable();

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.UserName!.Contains(search) ||
                    u.Email!.Contains(search) ||
                    (u.DisplayName != null && u.DisplayName.Contains(search))
                );
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = new List<UserProfileDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(new UserProfileDto
                {
                    Id = user.Id,
                    UserName = user.UserName ?? "",
                    Email = user.Email,
                    DisplayName = user.DisplayName,
                    Avatar = user.Avatar,
                    Role = roles.FirstOrDefault(),
                    CreatedAt = user.CreatedAt,
                    IsVerified = user.IsVerified,
                    IsBanned = user.IsBanned
                });
            }

            return Ok(new
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        /// <summary>
        /// Admin: Lấy chi tiết user
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Lấy chi tiết một user",
            Description = "Bao gồm thông tin bảo mật, trạng thái khoá, xác minh, doanh thu,..."
        )]
        [HttpGet("admin/{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetUserDetail(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new UserDetailDto
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled,
                AccessFailedCount = user.AccessFailedCount,
                LockoutEnd = user.LockoutEnd?.UtcDateTime,
                DisplayName = user.DisplayName,
                Bio = user.Bio,
                Avatar = user.Avatar,
                Role = roles.FirstOrDefault(),
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsVerified = user.IsVerified,
                IsBanned = user.IsBanned,
                Balance = user.Balance,
                TotalEarnings = user.TotalEarnings,
                TotalProducts = user.TotalProducts,
                TotalSales = user.TotalSales,
                BanReason = user.BanReason,
                BannedUntil = user.BannedUntil,
                BannedBy = user.BannedBy,
                VerificationRequested = user.VerificationRequested,
                VerificationStatus = user.VerificationStatus,
                VerifiedAt = user.VerifiedAt
            });
        }

        /// <summary>
        /// Admin: Ban user
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Ban user",
            Description = "Ban tạm thời hoặc vĩnh viễn. Lưu lại lý do & người ban."
        )]
        [HttpPost("admin/{id}/ban")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> BanUser(string id, [FromBody] BanUserDto dto)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            user.IsBanned = true;
            user.BanReason = dto.Reason;
            user.BannedBy = adminId;

            if (dto.DurationDays.HasValue)
            {
                user.BannedUntil = DateTime.UtcNow.AddDays(dto.DurationDays.Value);
            }
            else
            {
                user.BannedUntil = null; // Permanent ban
            }

            await _userManager.UpdateAsync(user);

            _logger.LogInformation("[BanUser] Admin {AdminName} banned user {UserId}. Reason: {Reason}",
                adminName, id, dto.Reason);

            return Ok(new
            {
                Message = $"User {user.UserName} has been banned",
                Reason = dto.Reason,
                BannedUntil = user.BannedUntil,
                BannedBy = adminName
            });
        }

        /// <summary>
        /// Admin: Unban user
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Unban user",
            Description = "Xóa trạng thái ban và khôi phục quyền truy cập."
        )]
        [HttpPost("admin/{id}/unban")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> UnbanUser(string id)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            var user = await _userManager.FindByIdAsync(id);

            if (user == null)
                return NotFound();

            user.IsBanned = false;
            user.BanReason = null;
            user.BannedUntil = null;
            user.BannedBy = null;

            await _userManager.UpdateAsync(user);

            _logger.LogInformation("[UnbanUser] Admin {AdminName} unbanned user {UserId}", adminName, id);

            return Ok(new { Message = $"User {user.UserName} has been unbanned" });
        }

        /// <summary>
        /// Admin: Approve seller verification
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Duyệt seller verification",
            Description = "Xác minh người bán và cập nhật trạng thái Approved."
        )]
        [HttpPost("admin/sellers/{id}/verify")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> VerifySeller(string id, [FromBody] ApproveVerificationDto? dto)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            var seller = await _userManager.FindByIdAsync(id);

            if (seller == null)
                return NotFound();

            seller.IsVerified = true;
            seller.VerificationStatus = "Approved";
            seller.VerifiedAt = DateTime.UtcNow;
            seller.VerifiedBy = adminId;
            seller.VerificationRejectionReason = null;

            await _userManager.UpdateAsync(seller);

            _logger.LogInformation("[VerifySeller] Admin {AdminName} approved verification for seller {SellerId}",
                adminName, id);

            // TODO: Send notification to seller

            return Ok(new
            {
                Message = $"Seller {seller.UserName} has been verified",
                VerifiedAt = seller.VerifiedAt,
                VerifiedBy = adminName
            });
        }

        /// <summary>
        /// Admin: Reject seller verification
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Từ chối xác minh seller",
            Description = "Ghi lý do và đặt trạng thái thành Rejected."
        )]
        [HttpPost("admin/sellers/{id}/reject-verification")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> RejectVerification(string id, [FromBody] RejectVerificationDto dto)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            var seller = await _userManager.FindByIdAsync(id);

            if (seller == null)
                return NotFound();

            seller.VerificationStatus = "Rejected";
            seller.VerificationRejectionReason = dto.Reason;
            seller.VerificationRequested = false;

            await _userManager.UpdateAsync(seller);

            _logger.LogInformation("[RejectVerification] Admin {AdminName} rejected verification for seller {SellerId}. Reason: {Reason}",
                adminName, id, dto.Reason);
            // TODO: Send notification to seller

            return Ok(new
            {
                Message = "Verification request rejected",
                Reason = dto.Reason
            });
        }

        /// <summary>
        /// Admin: Get pending verifications
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Danh sách seller đang chờ xác minh",
            Description = "Phân trang theo thời gian gửi yêu cầu."
        )]
        [HttpGet("admin/verifications/pending")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetPendingVerifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _userManager.Users
                .Where(u => u.VerificationRequested && u.VerificationStatus == "Pending");

            var totalCount = await query.CountAsync();
            var sellers = await query
                .OrderBy(u => u.VerificationRequestedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.BusinessName,
                    u.TaxId,
                    u.BusinessAddress,
                    u.BusinessLicenseUrl,
                    u.IdCardUrl,
                    u.VerificationRequestedAt
                })
                .ToListAsync();

            return Ok(new
            {
                Items = sellers,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Admin: Approve payout
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Chấp nhận yêu cầu rút tiền",
            Description = "Admin thực hiện chấp nhận yêu cầu rút tiền của Seller từ danh sách."
        )]
        [HttpPost("admin/payouts/{id}/approve")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> ApprovePayout(Guid id, [FromBody] ApprovePayoutDto? dto)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            var payout = await _dbContext.PayoutRequests.FindAsync(id);

            if (payout == null)
                return NotFound();

            if (payout.Status != PayoutStatus.Pending)
                return BadRequest(new { Message = "Payout is not in pending status" });

            payout.Status = PayoutStatus.Approved;
            payout.ProcessedAt = DateTime.UtcNow;
            payout.ProcessedBy = adminId;
            payout.Note = dto?.Note;

            // TODO: Integrate with payment gateway to transfer money

            // Mark as completed
            payout.Status = PayoutStatus.Completed;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("[ApprovePayout] Admin {AdminName} approved payout {PayoutId} for seller {SellerId}. Amount: {Amount}",
                adminName, id, payout.SellerId, payout.Amount);

            // TODO: Send notification to seller

            return Ok(new
            {
                Message = "Payout approved and processed",
                PayoutId = id,
                Amount = payout.Amount,
                ProcessedBy = adminName
            });
        }

        /// <summary>
        /// Admin: Reject payout
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Từ chối yêu cầu rút tiền",
            Description = "Admin thực hiện từ chối yêu cầu rút tiền của Seller từ danh sách."
        )]
        [HttpPost("admin/payouts/{id}/reject")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> RejectPayout(Guid id, [FromBody] RejectPayoutDto dto)
        {
            var adminId = HttpContext.GetUserId();
            var adminName = HttpContext.GetName();

            var payout = await _dbContext.PayoutRequests.FindAsync(id);

            if (payout == null)
                return NotFound();

            if (payout.Status != PayoutStatus.Pending)
                return BadRequest(new { Message = "Payout is not in pending status" });

            payout.Status = PayoutStatus.Rejected;
            payout.ProcessedAt = DateTime.UtcNow;
            payout.ProcessedBy = adminId;
            payout.RejectionReason = dto.Reason;

            // Refund balance to seller
            var seller = await _userManager.FindByIdAsync(payout.SellerId);
            if (seller != null)
            {
                seller.Balance += payout.Amount;
                await _userManager.UpdateAsync(seller);
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("[RejectPayout] Admin {AdminName} rejected payout {PayoutId}. Reason: {Reason}",
                adminName, id, dto.Reason);

            // TODO: Send notification to seller

            return Ok(new
            {
                Message = "Payout rejected, balance refunded to seller",
                Reason = dto.Reason
            });
        }

        /// <summary>
        /// Admin: Get pending payouts
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Lấy danh sách yêu cầu rút tiền",
            Description = "Trả về danh sách yêu cầu rút tiền đang đợi Admin xem xét."
        )]
        [HttpGet("admin/payouts/pending")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetPendingPayouts([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _dbContext.PayoutRequests
                .Where(p => p.Status == PayoutStatus.Pending);

            var totalCount = await query.CountAsync();
            var payouts = await query
                .OrderBy(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get seller names
            var sellerIds = payouts.Select(p => p.SellerId).Distinct().ToList();
            var sellers = await _userManager.Users
                .Where(u => sellerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UserName);

            var payoutDtos = payouts.Select(p => new PayoutRequestDto
            {
                Id = p.Id,
                SellerId = p.SellerId,
                SellerName = sellers.GetValueOrDefault(p.SellerId) ?? "Unknown",
                Amount = p.Amount,
                BankAccount = p.BankAccount,
                BankName = p.BankName,
                Status = p.Status.ToString(),
                CreatedAt = p.CreatedAt,
                ProcessedAt = p.ProcessedAt,
                ProcessedBy = p.ProcessedBy
            }).ToList();

            return Ok(new
            {
                Items = payoutDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            });
        }

        /// <summary>
        /// Admin: Get user statistics
        /// </summary>
        [SwaggerOperation(
            Summary = "Admin: Thống kê người dùng",
            Description = "Trả về các thông tin thống kê của người dùng."
        )]
        [HttpGet("admin/statistics")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetUserStatistics()
        {
            var totalUsers = await _userManager.Users.CountAsync();

            var buyers = 0;
            var sellers = 0;
            var verifiedSellers = 0;

            foreach (var user in await _userManager.Users.ToListAsync())
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("buyer")) buyers++;
                if (roles.Contains("seller"))
                {
                    sellers++;
                    if (user.IsVerified) verifiedSellers++;
                }
            }

            var bannedUsers = await _userManager.Users.CountAsync(u => u.IsBanned);
            var activeUsers = await _userManager.Users.CountAsync(u => u.LastLoginAt >= DateTime.UtcNow.AddDays(-30));
            var pendingVerifications = await _userManager.Users.CountAsync(u => u.VerificationRequested && u.VerificationStatus == "Pending");
            var pendingPayouts = await _dbContext.PayoutRequests.CountAsync(p => p.Status == PayoutStatus.Pending);

            return Ok(new UserStatisticsDto
            {
                TotalUsers = totalUsers,
                TotalBuyers = buyers,
                TotalSellers = sellers,
                VerifiedSellers = verifiedSellers,
                ActiveUsers = activeUsers,
                BannedUsers = bannedUsers,
                PendingVerifications = pendingVerifications,
                PendingPayouts = pendingPayouts
            });
        }
    }

}
