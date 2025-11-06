using System.Security.Claims;

namespace Services.BasketService.API.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetUserId(this HttpContext ctx)
        {
            return ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? ctx.User.FindFirst("sub")?.Value
                   ?? throw new UnauthorizedAccessException("Missing user id in token");
        }
    }
}
