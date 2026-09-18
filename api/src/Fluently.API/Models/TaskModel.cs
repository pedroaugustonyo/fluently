using Fluently.API.Enums;

namespace Fluently.API.Models;

/// <summary>
/// Tarefa pertencente a um usuário.
/// </summary>
public sealed class TaskModel : BaseModel
{
    /// <summary>
    /// Identificador do proprietário da tarefa.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Proprietário da tarefa.
    /// </summary>
    public required UserModel User { get; set; }

    /// <summary>
    /// Título da tarefa.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Prioridade da tarefa.
    /// </summary>
    public TaskPriorityEnum Priority { get; set; }

    /// <summary>
    /// Data de vencimento opcional.
    /// </summary>
    public DateOnly? DueDate { get; set; }

    /// <summary>
    /// Indica se a tarefa foi concluída.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Momento em que a tarefa foi concluída.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
