using FluentValidation;
using HCM.Application.Auth.DTOs;
using HCM.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HCM.Application.Auth.DataHandlers;

public class AuthDataHandler : IAuthDataHandler
{
    private readonly IApplicationDbContext _db;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthDataHandler> _logger;

    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public AuthDataHandler(IApplicationDbContext db, ITokenService tokenService, ILogger<AuthDataHandler> logger)
    {
        _db = db;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        ValidateLoginRequest(username, password);

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            if (user is not null)
                await RecordFailedAttempt(user, cancellationToken);

            // Generic message — do not reveal whether username exists
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        if (user.IsLockedOut)
            throw new UnauthorizedAccessException($"Account locked. Try again after {user.LockoutEnd:HH:mm} UTC.");

        // Reset failed attempts on successful login
        if (user.FailedLoginAttempts > 0)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEnd = null;
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();

        var refreshToken = new HCM.Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshTokenValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        _db.RefreshTokens.Add(refreshToken);
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {Username} logged in successfully", user.Username);

        return new AuthResponseDto(
            accessToken,
            refreshTokenValue,
            _tokenService.GetAccessTokenExpiry(),
            user.Username,
            user.Email,
            roles
        );
    }

    private void ValidateLoginRequest(string username, string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(username))
            errors.Add("Username is required.");
        else if (username.Length > 100)
            errors.Add("Username must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(password))
            errors.Add("Password is required.");
        else if (password.Length > 200)
            errors.Add("Password must not exceed 200 characters.");

        if (errors.Count > 0)
            throw new ValidationException(errors.Select(e => new FluentValidation.Results.ValidationFailure(nameof(username), e)));
    }

    private async Task RecordFailedAttempt(HCM.Domain.Entities.User user, CancellationToken cancellationToken)
    {
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= MaxFailedAttempts)
        {
            user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
            _logger.LogWarning("User {Username} locked out after {Attempts} failed attempts", user.Username, user.FailedLoginAttempts);
        }
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}
