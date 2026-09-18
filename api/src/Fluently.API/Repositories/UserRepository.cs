using Fluently.API.Data.Context;
using Fluently.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Fluently.API.Repositories;

/// <summary>
/// Operações de persistência específicas de usuários.
/// </summary>
public sealed class UserRepository(AppDbContext dbContext) : BaseRepository<UserModel>(dbContext), IUserRepository
{
    /// <summary>
    /// Verifica se existe um usuário com o e-mail normalizado informado.
    /// </summary>
    /// <param name="normalizedEmail">Endereço de e-mail normalizado.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Valor que indica se o usuário existe.</returns>
    public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return DbSet.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    /// <summary>
    /// Obtém um usuário pelo endereço de e-mail normalizado.
    /// </summary>
    /// <param name="normalizedEmail">Endereço de e-mail normalizado.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Usuário encontrado ou valor nulo.</returns>
    public Task<UserModel?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return DbSet.SingleOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    /// <summary>
    /// Conta a quantidade total de usuários cadastrados.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade total de usuários.</returns>
    public Task<int> CountAsync(CancellationToken cancellationToken)
    {
        return DbSet.CountAsync(cancellationToken);
    }

    /// <summary>
    /// Conta os usuários disponíveis no ranking.
    /// </summary>
    /// <param name="search">Termo usado para filtrar o nome do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade de usuários.</returns>
    public Task<int> CountLeaderboardAsync(string? search, CancellationToken cancellationToken)
    {
        return GetLeaderboardUsers(search).CountAsync(cancellationToken);
    }

    /// <summary>
    /// Obtém uma página ordenada por experiência e pela data de criação em caso de empate.
    /// </summary>
    /// <param name="skip">Quantidade de usuários que serão ignorados.</param>
    /// <param name="take">Quantidade máxima de usuários retornados.</param>
    /// <param name="search">Termo usado para filtrar o nome do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Lista de usuários da página solicitada.</returns>
    public async Task<IReadOnlyList<UserModel>> GetLeaderboardPageAsync(
        int skip,
        int take,
        string? search,
        CancellationToken cancellationToken
    )
    {
        return await GetLeaderboardUsers(search)
            .AsNoTracking()
            .OrderByDescending(user => user.TotalXp)
            .ThenBy(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    private IQueryable<UserModel> GetLeaderboardUsers(string? search)
    {
        var users = DbSet.AsQueryable();

        if (string.IsNullOrWhiteSpace(search))
        {
            return users;
        }

        var normalizedSearch = search.Trim().ToLower();

        return users.Where(user =>
            user.FirstName.ToLower().Contains(normalizedSearch)
            || user.LastName.ToLower().Contains(normalizedSearch)
            || (user.FirstName + " " + user.LastName).ToLower().Contains(normalizedSearch)
        );
    }
}
