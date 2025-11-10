using Duende.IdentityServer.Models;
using IdentityModel;
using Microsoft.AspNetCore.Identity;
using Services.IdentityService.Data;
using System.Security.Claims;

namespace Services.IdentityService.Security
{
    public class ProfileService
    {
        private readonly UserManager<AppUser> _userManager;

        public ProfileService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var user = await _userManager.GetUserAsync(context.Subject);
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Email, user.Email ?? ""),
                new Claim(JwtClaimTypes.Name, user.UserName ?? "")
            };

            claims.AddRange(roles.Select(r => new Claim(JwtClaimTypes.Role, r)));

            context.IssuedClaims.AddRange(claims);
        }
    }
}
