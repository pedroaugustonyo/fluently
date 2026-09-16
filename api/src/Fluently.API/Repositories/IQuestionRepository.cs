using Fluently.API.Models;

namespace Fluently.API.Repositories;

/// <summary>
/// Operações de persistência específicas de questões.
/// </summary>
public interface IQuestionRepository : IBaseRepository<QuestionModel>
{
    /// <summary>
    /// Obtém a questão pendente de um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão pendente ou valor nulo.</returns>
    Task<QuestionModel?> GetCurrentAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Remove a questão pendente de um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade de questões removidas.</returns>
    Task<int> DeleteCurrentAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém uma questão pertencente ao usuário informado.
    /// </summary>
    /// <param name="questionId">Identificador da questão.</param>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão encontrada ou valor nulo.</returns>
    Task<QuestionModel?> GetOwnedAsync(Guid questionId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Conta as questões pertencentes a um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade total de questões do usuário.</returns>
    Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém uma página de questões pertencentes a um usuário.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="skip">Quantidade de questões que serão ignoradas.</param>
    /// <param name="take">Quantidade máxima de questões retornadas.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Lista de questões da página solicitada.</returns>
    Task<IReadOnlyList<QuestionModel>> GetPageByUserAsync(Guid userId,
                                                          int skip,
                                                          int take,
                                                          CancellationToken cancellationToken);

    /// <summary>
    /// Obtém uma questão pertencente ao usuário informado.
    /// </summary>
    /// <param name="questionId">Identificador da questão.</param>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Questão encontrada ou valor nulo.</returns>
    Task<QuestionModel?> GetByIdAsync(Guid questionId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Verifica se o usuário já recebeu um contexto equivalente.
    /// </summary>
    /// <param name="userId">Identificador do usuário.</param>
    /// <param name="fingerprint">Impressão digital do contexto.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Valor que indica se o contexto já existe.</returns>
    Task<bool> ExistsContextFingerprintAsync(Guid userId,
                                             string fingerprint,
                                             CancellationToken cancellationToken);

}
