namespace Eventrian.Api.Features.Auth.Models;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Token { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; } // For audit/debug
    public ApplicationUser User { get; set; } = default!;

    public bool IsPersistent { get; set; }
}

