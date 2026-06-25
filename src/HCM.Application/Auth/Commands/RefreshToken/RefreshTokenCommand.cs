using HCM.Application.Auth.DTOs;
using MediatR;

namespace HCM.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string Token) : IRequest<AuthResponseDto>;
