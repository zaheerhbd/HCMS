namespace HCM.Application.CareTeam.DTOs;

public class AddCareTeamMemberDto
{
    public Guid UserId { get; set; }
    public string TeamRole { get; set; } = null!;
}
