using Fluently.API.DTOs.Common;
using Fluently.API.DTOs.Tasks;

namespace Fluently.API.Services;

/// <summary>
/// Define as operações de negócio das tarefas do usuário autenticado.
/// </summary>
public interface ITaskService
{
    /// <summary>
    /// Obtém uma tarefa do usuário autenticado.
    /// </summary>
    Task<TaskResponseDTO> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém as tarefas do usuário autenticado.
    /// </summary>
    /// <param name="request">Parâmetros de paginação e busca.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefas encontradas.</returns>
    Task<PaginatedResponseDTO<TaskResponseDTO>> PaginateAsync(
        PaginationRequestDTO request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Cria uma tarefa para o usuário autenticado.
    /// </summary>
    /// <param name="request">Dados da tarefa.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa criada.</returns>
    Task<TaskResponseDTO> CreateAsync(CreateTaskRequestDTO request, CancellationToken cancellationToken);

    /// <summary>
    /// Atualiza uma tarefa do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="request">Dados atualizados da tarefa.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa atualizada.</returns>
    Task<TaskResponseDTO> UpdateAsync(Guid id, UpdateTaskRequestDTO request, CancellationToken cancellationToken);

    /// <summary>
    /// Atualiza a conclusão de uma tarefa.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="request">Estado de conclusão.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa atualizada.</returns>
    Task<TaskResponseDTO> SetCompletionAsync(
        Guid id,
        TaskCompletionRequestDTO request,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Exclui uma tarefa do usuário autenticado.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa que representa a operação assíncrona.</returns>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
