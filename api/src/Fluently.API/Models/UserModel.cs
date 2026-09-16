using Fluently.API.Enums;

namespace Fluently.API.Models;

/// <summary>
/// Conta de usuário.
/// </summary>
public sealed class UserModel : BaseModel
{
    /// <summary>
    /// Primeiro nome.
    /// </summary>
    public required string FirstName { get; set; }

    /// <summary>
    /// Sobrenome.
    /// </summary>
    public required string LastName { get; set; }

    /// <summary>
    /// Endereço de e-mail.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// E-mail normalizado para comparação.
    /// </summary>
    public required string NormalizedEmail { get; set; }

    /// <summary>
    /// Hash da senha.
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Total de experiência acumulada.
    /// </summary>
    public long TotalXp { get; set; }

    /// <summary>
    /// Sequência atual de respostas corretas.
    /// </summary>
    public int CurrentStreak { get; set; }

    /// <summary>
    /// Nível atual de proficiência.
    /// </summary>
    public ProficiencyLevelEnum? Proficiency { get; set; }

    /// <summary>
    /// Biografia utilizada para personalizar as questões.
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Imagem de perfil codificada em Base64.
    /// </summary>
    public string? ProfileImageBase64 { get; set; }

    /// <summary>
    /// Questões geradas para o usuário.
    /// </summary>
    public ICollection<QuestionModel> Questions { get; } = [];
}
