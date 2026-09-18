using System.ComponentModel.DataAnnotations;
using Fluently.API.Enums;

namespace Fluently.API.DTOs.Tasks;

/// <summary>
/// Dados necessários para atualizar uma tarefa.
/// </summary>
public sealed class UpdateTaskRequestDTO
{
    /// <summary>
    /// Título da tarefa.
    /// </summary>
    [Required, StringLength(160)]
    public string? Title { get; init; }

    /// <summary>
    /// Prioridade da tarefa.
    /// </summary>
    [EnumDataType(typeof(TaskPriorityEnum))]
    public TaskPriorityEnum Priority { get; init; }

    /// <summary>
    /// Data de vencimento opcional.
    /// </summary>
    public DateOnly? DueDate { get; init; }
}
