namespace HCM.Application.Users.DTOs;

public class UpdateUserDto
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public class AssignRolesDto
{
    public List<string> Roles { get; set; } = new();
}

public class SetActiveStatusDto
{
    public bool IsActive { get; set; }
}
