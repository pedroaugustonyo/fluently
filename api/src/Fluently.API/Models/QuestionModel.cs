namespace Fluently.API.Models;

/// <summary>
/// Questão de aprendizagem gerada para um usuário.
/// </summary>
public sealed class QuestionModel : BaseModel
{
    /// <summary>
    /// Identificador do usuário.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Usuário proprietário.
    /// </summary>
    public required UserModel User { get; set; }

    /// <summary>
    /// Contexto apresentado antes da questão.
    /// </summary>
    public required string Context { get; set; }

    /// <summary>
    /// Frase que deve ser completada.
    /// </summary>
    public required string Question { get; set; }

    /// <summary>
    /// Tradução da frase para português.
    /// </summary>
    public required string QuestionTranslation { get; set; }

    /// <summary>
    /// Impressão digital do contexto.
    /// </summary>
    public required string ContextFingerprint { get; set; }

    /// <summary>
    /// Cinco alternativas apresentadas ao usuário.
    /// </summary>
    public string[] Alternatives { get; set; } = [];

    /// <summary>
    /// Traduções das alternativas apresentadas ao usuário.
    /// </summary>
    public string[] AlternativeTranslations { get; set; } = [];

    /// <summary>
    /// Índice da alternativa correta.
    /// </summary>
    public int CorrectAlternativeIndex { get; set; }

    /// <summary>
    /// Experiência base da questão.
    /// </summary>
    public int BaseXp { get; set; } = 15;

    /// <summary>
    /// Índice da alternativa enviada pelo usuário.
    /// </summary>
    public int? SubmittedAlternativeIndex { get; set; }

    /// <summary>
    /// Indicador de resposta correta.
    /// </summary>
    public bool? IsCorrect { get; set; }

    /// <summary>
    /// Experiência concedida pela resposta.
    /// </summary>
    public int? AwardedXp { get; set; }

    /// <summary>
    /// Sequência alcançada após a resposta.
    /// </summary>
    public int? StreakAfterAnswer { get; set; }

    /// <summary>
    /// Data da resposta.
    /// </summary>
    public DateTimeOffset? AnsweredAt { get; set; }
}
