using HCM.Application.Interfaces;
using HCM.Domain.Entities;

namespace HCM.Application.Tests.Helpers;

// Deterministic stand-in for ITokenService — avoids pulling in real JWT signing for unit tests
// that only care about whether a token was issued, not its contents.
public class FakeTokenService : ITokenService
{
    public string GenerateAccessToken(User user, IEnumerable<string> roles) => $"access-token-for-{user.Username}";

    public string GenerateRefreshToken() => Guid.NewGuid().ToString("N");

    public DateTime GetAccessTokenExpiry() => DateTime.UtcNow.AddMinutes(15);
}
