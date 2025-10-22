using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Services.IdentityService.Data;
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

        return Ok(new { token });
    }


    [HttpPost]
    [Authorize]
    public  IActionResult TestAuth()
    {
        return Ok();
    }
}

public record RegisterDto(string Username, string Email, string Password);
public record LoginDto(string Email, string Password);
