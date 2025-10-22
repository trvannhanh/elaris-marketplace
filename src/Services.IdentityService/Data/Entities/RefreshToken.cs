

namespace Services.IdentityService.Data.Entities;
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; }
    public string UserId { get; set; } = default!;
    public AppUser User { get; set; } = default!;
}
