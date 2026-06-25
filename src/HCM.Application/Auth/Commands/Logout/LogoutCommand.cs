using MediatR;

namespace HCM.Application.Auth.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;
