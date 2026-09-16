using Fluently.API.Enums;

namespace Fluently.API.DTOs.Users;

/// <summary>
/// Dados do usuário retornados na autenticação.
/// </summary>
public sealed class LoginUserDetailsResponseDTO
{
    /// <summary>
    /// Identificador do usuário.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Primeiro nome.
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Sobrenome.
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Endereço de e-mail.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Pontuação total.
    /// </summary>
    public long TotalXp { get; init; }

    /// <summary>
    /// Sequência atual de acertos.
    /// </summary>
    public int CurrentStreak { get; init; }

    /// <summary>
    /// Nível de proficiência.
    /// </summary>
    public ProficiencyLevelEnum? Proficiency { get; init; }

    /// <summary>
    /// Biografia usada como contexto das questões.
    /// </summary>
    public string? Bio { get; init; }

    /// <summary>
    /// Imagem de perfil codificada em Base64.
    /// </summary>
    public string? ProfileImageBase64 { get; init; }

    /// <summary>
    /// Data de criação da conta.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
