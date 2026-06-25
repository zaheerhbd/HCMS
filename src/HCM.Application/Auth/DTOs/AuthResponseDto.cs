namespace HCM.Application.Auth.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    string Username,
    string Email,
    IEnumerable<string> Roles
);
