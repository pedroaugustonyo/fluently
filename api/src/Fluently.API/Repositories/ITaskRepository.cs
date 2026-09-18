using Fluently.API.Models;

namespace Fluently.API.Repositories;

/// <summary>
/// Operações de persistência das tarefas.
/// </summary>
public interface ITaskRepository : IBaseRepository<TaskModel>
{
    /// <summary>
    /// Conta as tarefas pertencentes ao usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="search">Termo usado para filtrar o título.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefas encontradas.</returns>
    Task<int> CountByUserAsync(Guid userId, string? search, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém uma página de tarefas pertencentes ao usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="skip">Quantidade de tarefas que serão ignoradas.</param>
    /// <param name="take">Quantidade máxima de tarefas retornadas.</param>
    /// <param name="search">Termo usado para filtrar o título.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefas encontradas.</returns>
    Task<IReadOnlyList<TaskModel>> GetPageByUserAsync(
        Guid userId,
        int skip,
        int take,
        string? search,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Obtém uma tarefa pertencente ao usuário.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa encontrada ou valor nulo.</returns>
    Task<TaskModel?> GetOwnedAsync(Guid id, Guid userId, CancellationToken cancellationToken);
}
