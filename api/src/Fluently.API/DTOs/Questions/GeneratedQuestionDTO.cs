namespace Fluently.API.DTOs.Questions;

/// <summary>
/// Questão validada pelo serviço de geração.
/// </summary>
public sealed class GeneratedQuestionDTO
{
    /// <summary>
    /// Contexto apresentado antes da questão.
    /// </summary>
    public string Context { get; init; } = string.Empty;

    /// <summary>
    /// Frase que deve ser completada.
    /// </summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>
    /// Tradução da frase para português.
    /// </summary>
    public string QuestionTranslation { get; init; } = string.Empty;

    /// <summary>
    /// Alternativas de resposta estruturadas.
    /// </summary>
    public IReadOnlyList<QuestionAlternativeOutputDTO> Alternatives { get; init; } = [];

    /// <summary>
    /// Índice da alternativa correta.
    /// </summary>
    public int CorrectAlternativeIndex { get; init; }

    /// <summary>
    /// Identificador do contexto utilizado na geração.
    /// </summary>
    public string ContextFingerprint { get; init; } = string.Empty;
}
