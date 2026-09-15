using System.ComponentModel.DataAnnotations;

namespace Fluently.API.DTOs.Questions;

/// <summary>
/// Alternativa enviada para uma questão.
/// </summary>
public sealed class SubmitQuestionAnswerRequestDTO
{
    /// <summary>
    /// Índice da alternativa selecionada.
    /// </summary>
    [Range(1, 5, ErrorMessage = "Informe um índice de alternativa entre 1 e 5.")]
    public required int AlternativeIndex { get; init; }
}
