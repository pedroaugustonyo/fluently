namespace Fluently.API.DTOs.Users;

/// <summary>
/// Resultado da autenticação do usuário.
/// </summary>
public sealed class LoginUserResponseDTO
{
    /// <summary>
    /// Token JWT utilizado nas requisições autenticadas.
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Tipo do token de acesso.
    /// </summary>
    public string TokenType { get; init; } = string.Empty;

    /// <summary>
    /// Data e hora de expiração do token.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Dados do usuário autenticado.
    /// </summary>
    public LoginUserDetailsResponseDTO User { get; init; } = new();
}
