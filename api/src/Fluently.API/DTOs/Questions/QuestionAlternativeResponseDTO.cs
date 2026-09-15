namespace Fluently.API.DTOs.Questions;

/// <summary>
/// Alternativa apresentada em uma questão.
/// </summary>
public sealed class QuestionAlternativeResponseDTO
{
    /// <summary>
    /// Índice usado para responder a questão.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Palavra em inglês.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Tradução da palavra para português.
    /// </summary>
    public string Translation { get; init; } = string.Empty;
}
