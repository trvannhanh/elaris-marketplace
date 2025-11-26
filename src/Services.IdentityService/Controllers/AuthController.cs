using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Services.IdentityService.Data;
using Swashbuckle.AspNetCore.Annotations;
using System.Net.Http.Headers;

namespace Services.IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IHttpClientFactory _http;
        private readonly UserManager<AppUser> _userManager;
        public AuthController(IHttpClientFactory http, UserManager<AppUser> userManager)
        {
            _http = http;
            _userManager = userManager;
        }

        /// <summary>
        /// Yêu cầu token bằng tài khoản người dùng (Resource Owner Password).
        /// Gọi trực tiếp /connect/token của IdentityServer.
        /// </summary>
        [SwaggerOperation(
            Summary = "Yêu cầu token đăng nhập",
            Description = "Thực hiện grant_type=password để lấy access token, refresh token từ IdentityServer."
        )]
        [HttpPost("token")]
        public async Task<IActionResult> RequestToken([FromForm] TokenRequestDto dto)
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = dto.ClientId,
                ["client_secret"] = dto.ClientSecret,
                ["username"] = dto.Username,
                ["password"] = dto.Password,
                ["scope"] = dto.Scope ?? "openid profile elaris.api offline_access"
            };

            var response = await client.PostAsync("http://identityservice:8080/connect/token", new FormUrlEncodedContent(form));
            var content = await response.Content.ReadAsStringAsync();

            return Content(content, "application/json");
        }


        /// <summary>
        /// Làm mới access token bằng refresh token.
        /// </summary>
        [SwaggerOperation(
            Summary = "Làm mới token",
            Description = "Thực hiện grant_type=refresh_token để lấy access token mới từ IdentityServer."
        )]
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromForm] RefreshRequestDto dto)
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = dto.ClientId,
                ["client_secret"] = dto.ClientSecret,
                ["refresh_token"] = dto.RefreshToken
            };

            var response = await client.PostAsync("http://identityservice:8080/connect/token", new FormUrlEncodedContent(form));
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }


        /// <summary>
        /// Đăng xuất khỏi IdentityServer.
        /// </summary>
        [SwaggerOperation(
            Summary = "Đăng xuất",
            Description = "Gọi endpoint endsession của IdentityServer để thực hiện logout."
        )]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromForm] LogoutRequestDto dto)
        {
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var form = new Dictionary<string, string>
            {
                ["client_id"] = dto.ClientId,
                ["logoutId"] = dto.LogoutId ?? ""
            };

            var response = await client.PostAsync("http://identityservice:8080/connect/endsession", new FormUrlEncodedContent(form));
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }


        /// <summary>
        /// Đăng ký tài khoản người dùng mới.
        /// </summary>
        [SwaggerOperation(
            Summary = "Tạo tài khoản",
            Description = "Tạo user mới trong hệ thống và tự động gán role 'buyer'."
        )]
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = new AppUser
            {
                UserName = dto.Username,
                Email = dto.Email
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "buyer");

            return Ok(new { message = "Registered successfully" });
        }
    }

    public record TokenRequestDto(string ClientId, string ClientSecret, string Username, string Password, string? Scope);
    public record RefreshRequestDto(string ClientId, string ClientSecret, string RefreshToken);
    public record LogoutRequestDto(string ClientId, string? LogoutId);
    public record RegisterDto(string Username, string Email, string Password);
}
