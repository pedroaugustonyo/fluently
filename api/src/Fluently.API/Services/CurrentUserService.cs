using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Fluently.API.Exceptions;

namespace Fluently.API.Services;

/// <summary>
/// Dados do usuário autenticado na requisição atual.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    /// <summary>
    /// Acessor do contexto da requisição.
    /// </summary>
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Inicializa uma nova instância do serviço do usuário atual.
    /// </summary>
    /// <param name="httpContextAccessor">Acessor do contexto da requisição atual.</param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Obtém o identificador do usuário autenticado.
    /// </summary>
    /// <returns>Identificador do usuário autenticado.</returns>
    public Guid GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var value = user?.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedException(
                "Não foi possível identificar o usuário autenticado.");
        }

        return userId;
    }
}
