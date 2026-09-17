using Fluently.API.Models;

namespace Fluently.API.Repositories;

/// <summary>
/// Operações de persistência específicas de usuários.
/// </summary>
public interface IUserRepository : IBaseRepository<UserModel>
{
    /// <summary>
    /// Verifica se existe um usuário com o e-mail normalizado informado.
    /// </summary>
    /// <param name="normalizedEmail">Endereço de e-mail normalizado.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Valor que indica se o usuário existe.</returns>
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém um usuário pelo endereço de e-mail normalizado.
    /// </summary>
    /// <param name="normalizedEmail">Endereço de e-mail normalizado.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Usuário encontrado ou valor nulo.</returns>
    Task<UserModel?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>
    /// Conta a quantidade total de usuários cadastrados.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade total de usuários.</returns>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Conta os usuários que possuem experiência no ranking.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade de usuários com experiência.</returns>
    Task<int> CountLeaderboardAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtém uma página ordenada por experiência e pela data de criação em caso de empate.
    /// </summary>
    /// <param name="skip">Quantidade de usuários que serão ignorados.</param>
    /// <param name="take">Quantidade máxima de usuários retornados.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Lista de usuários da página solicitada.</returns>
    Task<IReadOnlyList<UserModel>> GetLeaderboardPageAsync(int skip,
                                                           int take,
                                                           CancellationToken cancellationToken);
}
