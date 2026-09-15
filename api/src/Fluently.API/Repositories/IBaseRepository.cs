using Fluently.API.Models;

namespace Fluently.API.Repositories;

/// <summary>
/// Operações básicas de persistência para uma entidade.
/// </summary>
/// <typeparam name="TModel">Tipo da entidade persistida.</typeparam>
public interface IBaseRepository<TModel>
    where TModel : BaseModel
{
    /// <summary>
    /// Obtém uma entidade pelo identificador.
    /// </summary>
    /// <param name="id">Identificador da entidade.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Entidade encontrada ou valor nulo.</returns>
    Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Adiciona uma entidade ao contexto de persistência.
    /// </summary>
    /// <param name="model">Entidade que será adicionada.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa que representa a operação assíncrona.</returns>
    Task AddAsync(TModel model, CancellationToken cancellationToken);

    /// <summary>
    /// Marca uma entidade como alterada no contexto de persistência.
    /// </summary>
    /// <param name="model">Entidade que será atualizada.</param>
    void Update(TModel model);

    /// <summary>
    /// Persiste as alterações pendentes no banco de dados.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade de registros afetados.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
