namespace Fluently.API.DTOs.Questions;

/// <summary>
/// Questão do usuário.
/// </summary>
public sealed class QuestionDetailsResponseDTO
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
    /// Tradução da frase após a resposta.
    /// </summary>
    public string? QuestionTranslation { get; init; }

    /// <summary>
    /// Alternativa correta após a resposta.
    /// </summary>
    public QuestionAlternativeResponseDTO? CorrectAlternative { get; init; }

    /// <summary>
    /// Experiência base da questão.
    /// </summary>
    public int BaseXp { get; init; }

    /// <summary>
    /// Índice da alternativa enviada pelo usuário.
    /// </summary>
    public int? SubmittedAlternativeIndex { get; init; }

    /// <summary>
    /// Indicador de resposta correta.
    /// </summary>
    public bool? IsCorrect { get; init; }

    /// <summary>
    /// Experiência concedida pela resposta.
    /// </summary>
    public int? AwardedXp { get; init; }

    /// <summary>
    /// Data e hora da criação.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Data e hora da resposta.
    /// </summary>
    public DateTimeOffset? AnsweredAt { get; init; }
}
