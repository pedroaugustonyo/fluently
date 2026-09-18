using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Fluently.API.Exceptions;

namespace Fluently.API.Services;

/// <summary>
/// Dados do usuário autenticado na requisição atual.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    /// <summary>
    /// Obtém o identificador do usuário autenticado.
    /// </summary>
    /// <returns>Identificador do usuário autenticado.</returns>
    public Guid GetUserId()
    {
        var user = httpContextAccessor.HttpContext?.User;
        var value =
            user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedException("Não foi possível identificar o usuário autenticado.");
        }

        return userId;
    }
}
