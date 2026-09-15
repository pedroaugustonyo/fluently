namespace Fluently.API.DTOs.Questions;

/// <summary>
/// Questão apresentada ao estudante.
/// </summary>
public sealed class QuestionResponseDTO
{
    /// <summary>
    /// Identificador da questão.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Contexto apresentado antes da questão.
    /// </summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>
    /// Frase que deve ser completada.
    /// </summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>
    /// Cinco alternativas de resposta.
    /// </summary>
    public IReadOnlyList<QuestionAlternativeResponseDTO> Alternatives { get; init; } = [];

    /// <summary>
    /// Experiência base da questão.
    /// </summary>
    public int BaseXp { get; init; }

    /// <summary>
    /// Data e hora da criação.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
