using FluentValidation;
using HCM.Application.Interfaces;
using HCM.Application.Users.DTOs;
using HCM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Users.DataHandlers;

public class UserDataHandler : IUserDataHandler
{
    private readonly IApplicationDbContext _db;

    public UserDataHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    // Returns paginated list of users, optionally filtered by name/username/email
    public async Task<UserListDto> GetUsersAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new UserListDto
        {
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            Items = items.Select(MapToDto).ToList()
        };
    }

    // Returns a single user by ID, throws KeyNotFoundException if not found
    public async Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        return MapToDto(user);
    }

    // Creates a new user with hashed password and assigned roles; throws on duplicate username/email
    public async Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        ValidateCreateInput(dto);

        if (await _db.Users.AnyAsync(u => u.Username == dto.Username, ct))
            throw new ValidationException($"Username '{dto.Username}' is already taken.");

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email, ct))
            throw new ValidationException($"Email '{dto.Email}' is already in use.");

        var validRoles = await ValidateRoleNamesAsync(dto.Roles, ct);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username.Trim(),
            Email = dto.Email.Trim(),
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);

        foreach (var role in validRoles)
            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        await _db.SaveChangesAsync(ct);

        return await GetUserByIdAsync(user.Id, ct);
    }

    // Updates name and email fields; username is immutable
    public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email.Trim() != user.Email)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email.Trim() && u.Id != id, ct))
                throw new ValidationException($"Email '{dto.Email}' is already in use.");
            user.Email = dto.Email.Trim();
        }

        if (!string.IsNullOrWhiteSpace(dto.FirstName))
            user.FirstName = dto.FirstName.Trim();

        if (!string.IsNullOrWhiteSpace(dto.LastName))
            user.LastName = dto.LastName.Trim();

        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return MapToDto(user);
    }

    // Replaces all existing role assignments with the provided list
    public async Task<UserDto> AssignRolesAsync(Guid id, IEnumerable<string> roles, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        var roleList = roles.ToList();
        var validRoles = await ValidateRoleNamesAsync(roleList, ct);

        var existing = _db.UserRoles.Where(ur => ur.UserId == id);
        _db.UserRoles.RemoveRange(existing);

        foreach (var role in validRoles)
            _db.UserRoles.Add(new UserRole { UserId = id, RoleId = role.Id });

        await _db.SaveChangesAsync(ct);

        return await GetUserByIdAsync(id, ct);
    }

    // Activates or deactivates a user account
    public async Task<UserDto> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException($"User {id} not found.");

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return MapToDto(user);
    }

    // Validates required fields and password strength for user creation
    private static void ValidateCreateInput(CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new ValidationException("Username is required.");
        if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains('@'))
            throw new ValidationException("A valid email address is required.");
        if (string.IsNullOrWhiteSpace(dto.FirstName))
            throw new ValidationException("First name is required.");
        if (string.IsNullOrWhiteSpace(dto.LastName))
            throw new ValidationException("Last name is required.");
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 8)
            throw new ValidationException("Password must be at least 8 characters.");
    }

    // Resolves role names to Role entities; throws if any name is invalid
    private async Task<List<Role>> ValidateRoleNamesAsync(IEnumerable<string> roleNames, CancellationToken ct)
    {
        var names = roleNames.ToList();
        if (names.Count == 0)
            return new List<Role>();

        var roles = await _db.Roles.Where(r => names.Contains(r.Name)).ToListAsync(ct);
        var invalid = names.Except(roles.Select(r => r.Name)).ToList();

        if (invalid.Any())
            throw new ValidationException($"Invalid role(s): {string.Join(", ", invalid)}.");

        return roles;
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName,
        IsActive = user.IsActive,
        Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
        CreatedAt = user.CreatedAt
    };
}
