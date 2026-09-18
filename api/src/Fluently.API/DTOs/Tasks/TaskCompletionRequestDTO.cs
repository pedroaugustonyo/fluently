using System.ComponentModel.DataAnnotations;

namespace Fluently.API.DTOs.Tasks;

/// <summary>
/// Estado de conclusão de uma tarefa.
/// </summary>
public sealed class TaskCompletionRequestDTO
{
    /// <summary>
    /// Indica se a tarefa está concluída.
    /// </summary>
    [Required]
    public bool? IsCompleted { get; init; }
}
