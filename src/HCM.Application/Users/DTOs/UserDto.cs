namespace HCM.Application.Users.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class UserListDto
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<UserDto> Items { get; set; } = new();
}
