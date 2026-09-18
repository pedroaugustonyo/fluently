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
public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenService
{
    /// <summary>
    /// Cria um token de acesso para o usuário informado.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="email">Endereço de e-mail do usuário.</param>
    /// <returns>Token de acesso emitido e sua expiração.</returns>
    public AccessTokenResultDTO Create(Guid userId, string email)
    {
        var now = timeProvider.GetUtcNow();
        var jwtOptions = options.Value;
        var expiresAt = now.AddMinutes(jwtOptions.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
        };
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var token = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256)
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new AccessTokenResultDTO { AccessToken = accessToken, ExpiresAt = expiresAt };
    }
}
