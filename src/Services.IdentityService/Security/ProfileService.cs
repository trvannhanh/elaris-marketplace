using Duende.IdentityServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Services.IdentityService.Data;
using System.Security.Claims;

namespace Services.IdentityService.Security
{
    /// <summary>
    /// Service xử lý thông tin profile của user cho IdentityServer
    /// Chịu trách nhiệm thêm các claims (thông tin xác thực) vào token
    /// </summary>
    public class ProfileService
    {
        private readonly UserManager<AppUser> _userManager;

        public ProfileService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            // Lấy thông tin user từ database dựa trên Subject (user identifier) trong context
            var user = await _userManager.GetUserAsync(context.Subject);

            // Lấy danh sách các roles (vai trò) của user
            var roles = await _userManager.GetRolesAsync(user);

            // Tạo danh sách claims cơ bản chứa email và username
            var claims = new List<Claim>
            {
                // Claim email - sử dụng "" nếu email null
                new Claim(JwtClaimTypes.Email, user.Email ?? ""),

                // Claim name - sử dụng "" nếu username null
                new Claim(JwtClaimTypes.Name, user.UserName ?? "")
            };

            // Thêm các role claims - mỗi role sẽ là một claim riêng biệt
            // Ví dụ: nếu user có role "Admin" và "User" thì sẽ có 2 claims role
            claims.AddRange(roles.Select(r => new Claim(JwtClaimTypes.Role, r)));

            // Thêm tất cả claims vào context để IdentityServer đưa vào token
            context.IssuedClaims.AddRange(claims);
        }
    }
}
