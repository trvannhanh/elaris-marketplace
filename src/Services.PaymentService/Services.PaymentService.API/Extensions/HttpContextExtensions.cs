using System.Security.Claims;

namespace Services.PaymentService.API.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetUserId(this HttpContext ctx)
        {
            return ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? ctx.User.FindFirst("sub")?.Value
                   ?? throw new UnauthorizedAccessException("Missing user id in token");
        }



        /// <summary>
        /// Lấy name của user 
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Nếu không có name</exception>
        public static string GetName(this HttpContext ctx)
        {
            var roleClaim = ctx.User.FindFirst(ClaimTypes.Name)
                         ?? ctx.User.FindFirst("name");

            return roleClaim?.Value
                   ?? throw new UnauthorizedAccessException("Missing name in token");
        }

        /// <summary>
        /// Lấy role duy nhất của user (nếu user chỉ có 1 role)
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Nếu không có role</exception>
        /// <exception cref="InvalidOperationException">Nếu user có nhiều hơn 1 role</exception>
        public static string GetRole(this HttpContext ctx)
        {
            var roleClaim = ctx.User.FindFirst(ClaimTypes.Role)
                         ?? ctx.User.FindFirst("role");

            return roleClaim?.Value
                   ?? throw new UnauthorizedAccessException("Missing role in token");
        }

        /// <summary>
        /// Lấy danh sách tất cả roles của user (hỗ trợ multiple roles)
        /// </summary>
        public static IEnumerable<string> GetRoles(this HttpContext ctx)
        {
            var roles = ctx.User.FindAll(ClaimTypes.Role)
                              .Select(c => c.Value)
                              .Concat(ctx.User.FindAll("role").Select(c => c.Value))
                              .Distinct()
                              .ToList();

            return roles.Any() ? roles : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Kiểm tra user có role cụ thể hay không
        /// </summary>
        public static bool HasRole(this HttpContext ctx, string role)
        {
            return ctx.User.IsInRole(role) || ctx.User.HasClaim("role", role);
        }

        /// <summary>
        /// Lấy role duy nhất (an toàn hơn GetRole - trả về null nếu không có hoặc nhiều role)
        /// </summary>
        public static string? GetRoleSafe(this HttpContext ctx)
        {
            var roles = ctx.GetRoles().ToList();
            return roles.Count == 1 ? roles[0] : null;
        }
    }
}