using Fluently.API.Data.Context;
using Fluently.API.Models;

using Microsoft.EntityFrameworkCore;

namespace Fluently.API.Repositories;

/// <summary>
/// Operações de persistência específicas de questões.
/// </summary>
public sealed class QuestionRepository : BaseRepository<QuestionModel>, IQuestionRepository
{
    /// <summary>
    /// Inicializa uma nova instância do repositório de questões.
    /// </summary>
    /// <param name="dbContext">Contexto utilizado para acessar o banco de dados.</param>
    public QuestionRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>
    /// Obtém a questão pendente de um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão pendente ou valor nulo.</returns>
    public Task<QuestionModel?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbSet.AsNoTracking()
            .SingleOrDefaultAsync(question => question.UserId == userId && question.AnsweredAt == null,
                                  cancellationToken);
    }

    /// <summary>
    /// Remove a questão pendente de um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade de questões removidas.</returns>
    public Task<int> DeleteCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbSet
            .Where(question => question.UserId == userId && question.AnsweredAt == null)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <summary>
    /// Obtém uma questão pertencente ao usuário informado.
    /// </summary>
    /// <param name="questionId">Identificador da questão.</param>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão encontrada ou valor nulo.</returns>
    public Task<QuestionModel?> GetOwnedAsync(Guid questionId,
                                              Guid userId,
                                              CancellationToken cancellationToken)
    {
        return _dbSet.Include(question => question.User)
            .SingleOrDefaultAsync(question => question.Id == questionId && question.UserId == userId,
                                  cancellationToken);
    }

    /// <summary>
    /// Conta as questões pertencentes a um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade total de questões do usuário.</returns>
    public Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _dbSet.CountAsync(question => question.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Obtém uma página de questões pertencentes a um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="skip">Quantidade de questões que serão ignoradas.</param>
    /// <param name="take">Quantidade máxima de questões retornadas.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Lista de questões da página solicitada.</returns>
    public async Task<IReadOnlyList<QuestionModel>> GetPageByUserAsync(Guid userId,
                                                                       int skip,
                                                                       int take,
                                                                       CancellationToken cancellationToken)
    {
        return await _dbSet.AsNoTracking()
            .Where(question => question.UserId == userId)
            .OrderByDescending(question => question.CreatedAt)
            .ThenByDescending(question => question.Id)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Obtém uma questão pertencente ao usuário informado.
    /// </summary>
    /// <param name="questionId">Identificador da questão.</param>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão encontrada ou valor nulo.</returns>
    public Task<QuestionModel?> GetByIdAsync(Guid questionId, Guid userId, CancellationToken cancellationToken)
    {
        return _dbSet.AsNoTracking()
            .SingleOrDefaultAsync(question => question.Id == questionId &&
                                              question.UserId == userId,
                                  cancellationToken);
    }

    /// <summary>
    /// Verifica se o usuário já recebeu um contexto equivalente.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="fingerprint">Impressão digital do contexto.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Valor que indica se o contexto já existe.</returns>
    public Task<bool> ExistsContextFingerprintAsync(Guid userId,
                                                     string fingerprint,
                                                     CancellationToken cancellationToken)
    {
        return _dbSet.AnyAsync(
            question =>
                question.UserId == userId &&
                question.ContextFingerprint == fingerprint,
            cancellationToken);
    }

    /// <summary>
    /// Obtém os contextos mais recentes de um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="limit">Quantidade máxima de contextos.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Lista dos contextos mais recentes.</returns>
    public async Task<IReadOnlyList<string>> GetRecentContextsAsync(Guid userId,
                                                                    int limit,
                                                                    CancellationToken cancellationToken)
    {
        return await _dbSet.AsNoTracking()
            .Where(question => question.UserId == userId)
            .OrderByDescending(question => question.CreatedAt)
            .Take(limit)
            .Select(question => question.Context)
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Obtém as questões erradas mais recentes de um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="limit">Quantidade máxima de questões.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Lista das questões erradas mais recentes.</returns>
    public async Task<IReadOnlyList<QuestionModel>> GetRecentIncorrectAsync(Guid userId,
                                                                             int limit,
                                                                             CancellationToken cancellationToken)
    {
        return await _dbSet.AsNoTracking()
            .Where(question => question.UserId == userId &&
                               question.AnsweredAt != null &&
                               question.IsCorrect == false)
            .OrderByDescending(question => question.AnsweredAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
    }
}
