using Fluently.API.Data.Context;
using Fluently.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Fluently.API.Repositories;

/// <summary>
/// Operações de persistência específicas de questões.
/// </summary>
public sealed class QuestionRepository(AppDbContext dbContext)
    : BaseRepository<QuestionModel>(dbContext),
        IQuestionRepository
{
    /// <summary>
    /// Obtém a experiência concedida no intervalo informado.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="start">Início inclusivo do intervalo.</param>
    /// <param name="end">Fim exclusivo do intervalo.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Experiência concedida.</returns>
    public async Task<int> GetAwardedXpAsync(
        Guid userId,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken
    )
    {
        return await DbSet
            .Where(question => question.UserId == userId && question.AnsweredAt >= start && question.AnsweredAt < end)
            .SumAsync(question => question.AwardedXp ?? 0, cancellationToken);
    }

    /// <summary>
    /// Obtém a questão pendente de um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão pendente ou valor nulo.</returns>
    public Task<QuestionModel?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        return DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(
                question => question.UserId == userId && question.AnsweredAt == null,
                cancellationToken
            );
    }

    /// <summary>
    /// Remove a questão pendente de um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade de questões removidas.</returns>
    public Task<int> DeleteCurrentAsync(Guid userId, CancellationToken cancellationToken)
    {
        return DbSet
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
    public Task<QuestionModel?> GetOwnedAsync(Guid questionId, Guid userId, CancellationToken cancellationToken)
    {
        return DbSet
            .Include(question => question.User)
            .SingleOrDefaultAsync(
                question => question.Id == questionId && question.UserId == userId,
                cancellationToken
            );
    }

    /// <summary>
    /// Conta as questões pertencentes a um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="search">Termo usado para filtrar as questões.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade total de questões do usuário.</returns>
    public Task<int> CountByUserAsync(Guid userId, string? search, CancellationToken cancellationToken)
    {
        return GetUserQuestions(userId, search).CountAsync(cancellationToken);
    }

    /// <summary>
    /// Obtém uma página de questões pertencentes a um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="skip">Quantidade de questões que serão ignoradas.</param>
    /// <param name="take">Quantidade máxima de questões retornadas.</param>
    /// <param name="search">Termo usado para filtrar as questões.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Lista de questões da página solicitada.</returns>
    public async Task<IReadOnlyList<QuestionModel>> GetPageByUserAsync(
        Guid userId,
        int skip,
        int take,
        string? search,
        CancellationToken cancellationToken
    )
    {
        return await GetUserQuestions(userId, search)
            .AsNoTracking()
            .OrderByDescending(question => question.CreatedAt)
            .ThenByDescending(question => question.Id)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<QuestionModel> GetUserQuestions(Guid userId, string? search)
    {
        var questions = DbSet.Where(question => question.UserId == userId);

        if (string.IsNullOrWhiteSpace(search))
        {
            return questions;
        }

        var normalizedSearch = search.Trim().ToLower();

        return questions.Where(question =>
            question.Question.ToLower().Contains(normalizedSearch)
            || question.QuestionTranslation.ToLower().Contains(normalizedSearch)
            || question.Context.ToLower().Contains(normalizedSearch)
        );
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
        return DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(
                question => question.Id == questionId && question.UserId == userId,
                cancellationToken
            );
    }

    /// <summary>
    /// Verifica se o usuário já recebeu um contexto equivalente.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="fingerprint">Impressão digital do contexto.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Valor que indica se o contexto já existe.</returns>
    public Task<bool> ExistsContextFingerprintAsync(
        Guid userId,
        string fingerprint,
        CancellationToken cancellationToken
    )
    {
        return DbSet.AnyAsync(
            question => question.UserId == userId && question.ContextFingerprint == fingerprint,
            cancellationToken
        );
    }
}
