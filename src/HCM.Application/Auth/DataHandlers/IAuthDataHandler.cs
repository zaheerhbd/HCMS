using HCM.Application.Auth.DTOs;

namespace HCM.Application.Auth.DataHandlers;

public interface IAuthDataHandler
{
    Task<AuthResponseDto> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
