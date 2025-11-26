// Services.IdentityService/Data/SeedData.cs
using Microsoft.AspNetCore.Identity;
using Services.IdentityService.Data;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        // ==================== SEED 3 ROLES ====================
        var roles = new[] { "buyer", "seller", "admin" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                Console.WriteLine($"[SeedData] ✅ Created role: {role}");
            }
        }

        // ==================== SEED TEST USERS ====================

        // Admin
        await CreateUserIfNotExists(
            userManager,
            username: "admin1",
            email: "admin@elaris.local",
            password: "Admin@123",
            roles: new[] { "admin" }
        );

        // Buyer
        await CreateUserIfNotExists(
            userManager,
            username: "buyer1",
            email: "buyer@elaris.local",
            password: "Buyer@123",
            roles: new[] { "buyer" }
        );

        // Seller
        await CreateUserIfNotExists(
            userManager,
            username: "seller1",
            email: "seller@elaris.local",
            password: "Seller@123",
            roles: new[] { "seller" }
        );

        Console.WriteLine("[SeedData] ✅ Seed data completed");
    }

    private static async Task CreateUserIfNotExists(
        UserManager<AppUser> userManager,
        string username,
        string email,
        string password,
        string[] roles)
    {
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser == null)
        {
            var user = new AppUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRolesAsync(user, roles);
                Console.WriteLine($"[SeedData] ✅ Created user: {username} with roles: {string.Join(", ", roles)}");
            }
            else
            {
                Console.WriteLine($"[SeedData] ❌ Failed to create user: {username}");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  - {error.Description}");
                }
            }
        }
    }
}