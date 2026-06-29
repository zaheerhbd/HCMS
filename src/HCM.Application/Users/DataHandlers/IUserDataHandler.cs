using HCM.Application.Users.DTOs;

namespace HCM.Application.Users.DataHandlers;

public interface IUserDataHandler
{
    Task<UserListDto> GetUsersAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto dto, CancellationToken ct = default);
    Task<UserDto> AssignRolesAsync(Guid id, IEnumerable<string> roles, CancellationToken ct = default);
    Task<UserDto> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken ct = default);
}
