using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Tasks;
using Fluently.API.Exceptions;
using Fluently.API.Helpers;
using Fluently.API.Models;
using Fluently.API.Repositories;

namespace Fluently.API.Services;

/// <summary>
/// Regras de negócio das tarefas do usuário.
/// </summary>
public sealed class TaskService(
    ICurrentUserService currentUserService,
    ITaskRepository taskRepository,
    TimeProvider timeProvider
) : ITaskService
{
    /// <summary>
    /// Obtém uma tarefa do usuário autenticado.
    /// </summary>
    public async Task<TaskResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Map(await GetOwnedAsync(id, currentUserService.GetUserId(), cancellationToken));
    }

    /// <summary>
    /// Obtém as tarefas do usuário autenticado.
    /// </summary>
    /// <param name="request">Parâmetros de paginação e busca.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefas encontradas.</returns>
    public async Task<PaginatedResponseDTO<TaskResponseDTO>> PaginateAsync(
        PaginationRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserService.GetUserId();
        var skip = PaginationHelper.CalculateSkip(request);
        var totalItems = await taskRepository.CountByUserAsync(userId, request.Search, cancellationToken);
        var tasks = await taskRepository.GetPageByUserAsync(
            userId,
            skip,
            request.PageSize,
            request.Search,
            cancellationToken
        );

        return PaginationHelper.CreateResponse(tasks.Select(Map).ToArray(), request, totalItems);
    }

    /// <summary>
    /// Cria uma tarefa para o usuário autenticado.
    /// </summary>
    /// <param name="request">Dados da tarefa.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa criada.</returns>
    public async Task<TaskResponseDTO> CreateAsync(CreateTaskRequestDTO request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetUserId();

        var task = new TaskModel
        {
            UserId = userId,
            User = null!,
            Title = request.Title!,
            Priority = request.Priority,
            DueDate = request.DueDate,
        };

        await taskRepository.AddAsync(task, cancellationToken);
        await taskRepository.SaveChangesAsync(cancellationToken);

        return Map(task);
    }

    /// <summary>
    /// Atualiza uma tarefa do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="request">Dados atualizados da tarefa.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa atualizada.</returns>
    public async Task<TaskResponseDTO> UpdateAsync(
        Guid id,
        UpdateTaskRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserService.GetUserId();
        var task = await GetOwnedAsync(id, userId, cancellationToken);

        task.Title = request.Title!;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;

        taskRepository.Update(task);
        await taskRepository.SaveChangesAsync(cancellationToken);

        return Map(task);
    }

    /// <summary>
    /// Atualiza a conclusão de uma tarefa.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="request">Estado de conclusão.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa atualizada.</returns>
    public async Task<TaskResponseDTO> SetCompletionAsync(
        Guid id,
        TaskCompletionRequestDTO request,
        CancellationToken cancellationToken
    )
    {
        var task = await GetOwnedAsync(id, currentUserService.GetUserId(), cancellationToken);

        task.IsCompleted = request.IsCompleted!.Value;
        task.CompletedAt = task.IsCompleted ? timeProvider.GetUtcNow() : null;

        taskRepository.Update(task);
        await taskRepository.SaveChangesAsync(cancellationToken);

        return Map(task);
    }

    /// <summary>
    /// Exclui uma tarefa do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa que representa a operação assíncrona.</returns>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await GetOwnedAsync(id, currentUserService.GetUserId(), cancellationToken);

        taskRepository.Remove(task);
        await taskRepository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Obtém uma tarefa pertencente ao usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa encontrada.</returns>
    private async Task<TaskModel> GetOwnedAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetOwnedAsync(id, userId, cancellationToken);

        return task ?? throw new NotFoundException("A tarefa não foi encontrada.");
    }

    /// <summary>
    /// Converte uma entidade de tarefa no contrato de resposta da API.
    /// </summary>
    /// <param name="task">Tarefa a converter.</param>
    /// <returns>Contrato de resposta da tarefa.</returns>
    private static TaskResponseDTO Map(TaskModel task)
    {
        return new TaskResponseDTO
        {
            Id = task.Id,
            Title = task.Title,
            Priority = task.Priority,
            DueDate = task.DueDate,
            IsCompleted = task.IsCompleted,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt,
        };
    }
}
