using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Services.IdentityService.Data
{
    public class AppUser : IdentityUser
    {
        // thêm field custom nếu cần (DisplayName, Avatar, ...)
    }

    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // configure thêm nếu cần
        }
    }
}