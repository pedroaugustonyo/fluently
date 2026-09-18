using Fluently.API.Enums;

namespace Fluently.API.DTOs.Tasks;

/// <summary>
/// Dados de uma tarefa retornada pela API.
/// </summary>
public sealed class TaskResponseDTO
{
    /// <summary>
    /// Identificador da tarefa.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Título da tarefa.
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Prioridade da tarefa.
    /// </summary>
    public TaskPriorityEnum Priority { get; init; }

    /// <summary>
    /// Data de vencimento.
    /// </summary>
    public DateOnly? DueDate { get; init; }

    /// <summary>
    /// Indica conclusão.
    /// </summary>
    public bool IsCompleted { get; init; }

    /// <summary>
    /// Momento da conclusão.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Momento da criação.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
