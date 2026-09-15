namespace Fluently.API.DTOs.Auth;

/// <summary>
/// Token de acesso do usuário autenticado.
/// </summary>
public sealed class AccessTokenResultDTO
{
    /// <summary>
    /// Token de acesso JWT.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Data e hora de expiração do token.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }
}
