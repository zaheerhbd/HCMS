using HCM.Application.Auth.DTOs;
using MediatR;

namespace HCM.Application.Auth.Commands.Login;

public record LoginCommand(string Username, string Password) : IRequest<AuthResponseDto>;
