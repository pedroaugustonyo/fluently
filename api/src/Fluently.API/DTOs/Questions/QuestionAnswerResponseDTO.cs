namespace Fluently.API.DTOs.Questions;

/// <summary>
/// Resultado da resposta enviada para uma questão.
/// </summary>
public sealed class QuestionAnswerResponseDTO
{
    /// <summary>
    /// Identificador da questão respondida.
    /// </summary>
    public Guid QuestionId { get; init; }

    /// <summary>
    /// Indicador de resposta correta.
    /// </summary>
    public bool IsCorrect { get; init; }

    /// <summary>
    /// Experiência concedida pela resposta.
    /// </summary>
    public int AwardedXp { get; init; }

    /// <summary>
    /// Experiência total acumulada pelo usuário.
    /// </summary>
    public long TotalXp { get; init; }

    /// <summary>
    /// Sequência atual de respostas corretas.
    /// </summary>
    public int CurrentStreak { get; init; }

    /// <summary>
    /// Alternativa correta da questão.
    /// </summary>
    public QuestionAlternativeResponseDTO CorrectAlternative { get; init; } = new();

    /// <summary>
    /// Tradução da frase para português.
    /// </summary>
    public string QuestionTranslation { get; init; } = string.Empty;

    /// <summary>
    /// Data e hora da resposta.
    /// </summary>
    public DateTimeOffset AnsweredAt { get; init; }
}
