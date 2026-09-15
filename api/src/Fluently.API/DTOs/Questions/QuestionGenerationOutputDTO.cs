namespace Fluently.API.DTOs.Questions;

/// <summary>
/// Saída estruturada da geração de uma questão.
/// </summary>
public sealed class QuestionGenerationOutputDTO
{
    /// <summary>
    /// Contexto apresentado antes da questão.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// Frase que deve ser completada.
    /// </summary>
    public required string Question { get; init; }

    /// <summary>
    /// Tradução da frase para português.
    /// </summary>
    public required string QuestionTranslation { get; init; }

    /// <summary>
    /// Cinco alternativas de resposta.
    /// </summary>
    public required IReadOnlyList<QuestionAlternativeOutputDTO> Alternatives { get; init; }

    /// <summary>
    /// Índice da alternativa correta.
    /// </summary>
    public required int CorrectAlternativeIndex { get; init; }
}
