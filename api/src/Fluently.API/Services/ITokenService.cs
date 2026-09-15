using Fluently.API.DTOs.Auth;

namespace Fluently.API.Services;

/// <summary>
/// Operações de emissão de tokens de acesso.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Cria um token de acesso para o usuário informado.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="email">Endereço de e-mail do usuário.</param>
    /// <returns>Token de acesso emitido e sua expiração.</returns>
    AccessTokenResultDTO Create(Guid userId, string email);
}
