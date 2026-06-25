using HCM.Application.Auth.DTOs;
using HCM.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(IApplicationDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await _db.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.Token, cancellationToken);

        if (existing is null || !existing.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        if (!existing.User.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        // Rotate: revoke old, issue new
        existing.IsRevoked = true;

        var newRefreshValue = _tokenService.GenerateRefreshToken();
        existing.ReplacedByToken = newRefreshValue;

        var roles = existing.User.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _tokenService.GenerateAccessToken(existing.User, roles);

        var newRefreshToken = new HCM.Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            Token = newRefreshValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto(
            accessToken,
            newRefreshValue,
            _tokenService.GetAccessTokenExpiry(),
            existing.User.Username,
            existing.User.Email,
            roles
        );
    }
}
