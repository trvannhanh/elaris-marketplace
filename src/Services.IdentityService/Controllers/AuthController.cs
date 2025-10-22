using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.IdentityService.Data;
using Services.IdentityService.Data.Entities;
using Services.IdentityService.Utils;

namespace Services.IdentityService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly JwtTokenGenerator _tokenGen;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        JwtTokenGenerator tokenGen)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenGen = tokenGen;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var user = new AppUser { UserName = dto.Username, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, "user");
        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null) return Unauthorized();

        var check = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!check.Succeeded) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenGen.GenerateToken(user, roles);
        var refreshToken = Guid.NewGuid().ToString("N");

        var entity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            Revoked = false
        };

        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.RefreshTokens.Add(entity);
        await db.SaveChangesAsync();

        return Ok(new { token, refreshToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest dto)
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stored = await db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == dto.RefreshToken && !x.Revoked);

        if (stored == null || stored.ExpiresAt < DateTime.UtcNow)
            return Unauthorized();

        stored.Revoked = true;
        await db.SaveChangesAsync();

        var roles = await _userManager.GetRolesAsync(stored.User);
        var newToken = _tokenGen.GenerateToken(stored.User, roles);
        var newRefresh = Guid.NewGuid().ToString("N");

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = newRefresh,
            UserId = stored.UserId,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        return Ok(new { token = newToken, refreshToken = newRefresh });
    }



    [HttpPost]
    [Authorize]
    public  IActionResult TestAuth()
    {
        return Ok();
    }


    [Authorize(Policy = "AdminOnly")]
    [HttpGet("admin-area")]
    public IActionResult AdminArea()
    {
        return Ok("You are admin!");
    }
}

public record RefreshRequest(string RefreshToken);

public record RegisterDto(string Username, string Email, string Password);
public record LoginDto(string Email, string Password);
