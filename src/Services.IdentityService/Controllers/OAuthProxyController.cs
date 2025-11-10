using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace Services.IdentityService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OAuthProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _http;

        public OAuthProxyController(IHttpClientFactory http)
        {
            _http = http;
        }

        /// <summary>
        /// Gọi trực tiếp endpoint /connect/token của IdentityServer
        /// </summary>
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
    }

    public record TokenRequestDto(
        string ClientId,
        string ClientSecret,
        string Username,
        string Password,
        string? Scope
    );
}
