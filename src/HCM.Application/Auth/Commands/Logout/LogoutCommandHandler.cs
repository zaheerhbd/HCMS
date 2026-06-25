using HCM.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HCM.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IApplicationDbContext _db;

    public LogoutCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (token is not null && token.IsActive)
        {
            token.IsRevoked = true;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
