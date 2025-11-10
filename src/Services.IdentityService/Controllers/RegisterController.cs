using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Services.IdentityService.Data;

namespace Services.IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;

        public RegisterController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        /// <summary>
        /// Đăng ký tài khoản mới (chỉ dùng nội bộ, không public OAuth)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new AppUser { UserName = dto.Username, Email = dto.Email };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "user");
            return Ok(new { message = "User registered successfully" });
        }
    }

    public record RegisterDto(string Username, string Email, string Password);
}
