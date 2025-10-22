using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Services.IdentityService.Data.Entities;

namespace Services.IdentityService.Data
{
    public class AppUser : IdentityUser
    {
        // thêm field custom nếu cần (DisplayName, Avatar, ...)
    }

    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // configure thêm nếu cần
        }
    }
}