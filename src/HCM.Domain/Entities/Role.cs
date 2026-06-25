namespace HCM.Domain.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Supervisor = "Supervisor";
    public const string CareCoordinator = "CareCoordinator";
    public const string Clinician = "Clinician";
    public const string ReadOnly = "ReadOnly";

    public static readonly IReadOnlyList<string> All =
        [Admin, Supervisor, CareCoordinator, Clinician, ReadOnly];
}
