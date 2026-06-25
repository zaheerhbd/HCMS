using HCM.Domain.Entities;

namespace HCM.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    DateTime GetAccessTokenExpiry();
}
