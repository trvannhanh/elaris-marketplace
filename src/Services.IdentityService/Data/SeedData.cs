using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Services.IdentityService.Data.AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var adminRole = "admin";
        var userRole = "user";
        if (!await roleManager.RoleExistsAsync(adminRole))
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        if (!await roleManager.RoleExistsAsync(userRole))
            await roleManager.CreateAsync(new IdentityRole(userRole));

        var adminEmail = "admin@elaris.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new Services.IdentityService.Data.AppUser { UserName = "admin", Email = adminEmail, EmailConfirmed = true };
            await userManager.CreateAsync(admin, "P@ssw0rd!"); // dev only; change later
            await userManager.AddToRoleAsync(admin, adminRole);
        }
    }
}
