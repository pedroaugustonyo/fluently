namespace Fluently.API.DTOs.Questions;

/// <summary>
/// Alternativa estruturada retornada pelo modelo de linguagem.
/// </summary>
public sealed class QuestionAlternativeOutputDTO
{
    /// <summary>
    /// Palavra em inglês.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Tradução da palavra para português.
    /// </summary>
    public required string Translation { get; init; }
}
