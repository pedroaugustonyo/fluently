using Fluently.API.Data.Context;
using Fluently.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Fluently.API.Repositories;

/// <summary>
/// Operações comuns de persistência das entidades.
/// </summary>
/// <typeparam name="TModel">Tipo da entidade persistida.</typeparam>
public class BaseRepository<TModel>(AppDbContext dbContext) : IBaseRepository<TModel>
    where TModel : BaseModel
{
    /// <summary>
    /// Conjunto de entidades do tipo persistido.
    /// </summary>
    protected DbSet<TModel> DbSet => dbContext.Set<TModel>();

    /// <summary>
    /// Obtém uma entidade pelo identificador.
    /// </summary>
    /// <param name="id">Identificador da entidade.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Entidade encontrada ou valor nulo.</returns>
    public async Task<TModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await DbSet.SingleOrDefaultAsync(model => model.Id == id, cancellationToken);
    }

    /// <summary>
    /// Adiciona uma entidade ao contexto de persistência.
    /// </summary>
    /// <param name="model">Entidade que será adicionada.</param>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Tarefa que representa a operação assíncrona.</returns>
    public async Task AddAsync(TModel model, CancellationToken cancellationToken)
    {
        await DbSet.AddAsync(model, cancellationToken);
    }

    /// <summary>
    /// Marca uma entidade como alterada no contexto de persistência.
    /// </summary>
    /// <param name="model">Entidade que será atualizada.</param>
    public void Update(TModel model)
    {
        DbSet.Update(model);
    }

    /// <summary>
    /// Marca uma entidade para remoção do contexto de persistência.
    /// </summary>
    /// <param name="model">Entidade que será removida.</param>
    public void Remove(TModel model)
    {
        DbSet.Remove(model);
    }

    /// <summary>
    /// Persiste as alterações pendentes no banco de dados.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade de registros afetados.</returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}
