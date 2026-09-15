using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Fluently.API.DTOs.Auth;
using Fluently.API.Options;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fluently.API.Services;

/// <summary>
/// Emissão de tokens JWT para usuários autenticados.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    /// <summary>
    /// Configurações JWT.
    /// </summary>
    private readonly IOptions<JwtOptions> _options;

    /// <summary>
    /// Provedor de data e hora.
    /// </summary>
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Inicializa uma nova instância do serviço de tokens.
    /// </summary>
    /// <param name="options">Configurações utilizadas na emissão dos tokens.</param>
    /// <param name="timeProvider">Provedor utilizado para obter a data e hora atuais.</param>
    public JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider)
    {
        _options = options;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Cria um token de acesso para o usuário informado.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="email">Endereço de e-mail do usuário.</param>
    /// <returns>Token de acesso emitido e sua expiração.</returns>
    public AccessTokenResultDTO Create(Guid userId, string email)
    {
        var now = _timeProvider.GetUtcNow();
        var jwtOptions = _options.Value;
        var expiresAt = now.AddMinutes(jwtOptions.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email)
        };
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResultDTO
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt
        };
    }
}
