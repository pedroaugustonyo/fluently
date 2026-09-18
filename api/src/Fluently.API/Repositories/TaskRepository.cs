using Fluently.API.Data.Context;
using Fluently.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Fluently.API.Repositories;

/// <summary>
/// Operações de persistência específicas das tarefas.
/// </summary>
public sealed class TaskRepository(AppDbContext dbContext) : BaseRepository<TaskModel>(dbContext), ITaskRepository
{
    /// <summary>
    /// Conta as tarefas pertencentes ao usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="search">Termo usado para filtrar o título.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefas encontradas.</returns>
    public Task<int> CountByUserAsync(Guid userId, string? search, CancellationToken cancellationToken)
    {
        return GetUserTasks(userId, search).CountAsync(cancellationToken);
    }

    /// <summary>
    /// Obtém uma página de tarefas pertencentes ao usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="skip">Quantidade de tarefas que serão ignoradas.</param>
    /// <param name="take">Quantidade máxima de tarefas retornadas.</param>
    /// <param name="search">Termo usado para filtrar o título.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefas encontradas.</returns>
    public async Task<IReadOnlyList<TaskModel>> GetPageByUserAsync(
        Guid userId,
        int skip,
        int take,
        string? search,
        CancellationToken cancellationToken
    )
    {
        return await GetUserTasks(userId, search)
            .AsNoTracking()
            .OrderBy(item => item.IsCompleted)
            .ThenByDescending(item => item.Priority)
            .ThenBy(item => item.DueDate)
            .ThenByDescending(item => item.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<TaskModel> GetUserTasks(Guid userId, string? search)
    {
        var tasks = DbSet.Where(item => item.UserId == userId);

        if (string.IsNullOrWhiteSpace(search))
        {
            return tasks;
        }

        var normalizedSearch = search.Trim().ToLower();

        return tasks.Where(item => item.Title.ToLower().Contains(normalizedSearch));
    }

    /// <summary>
    /// Obtém uma tarefa pertencente ao usuário.
    /// </summary>
    /// <param name="id">Identificador da tarefa.</param>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa encontrada ou valor nulo.</returns>
    public Task<TaskModel?> GetOwnedAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        return DbSet.SingleOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
    }
}
