using Fluently.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Fluently.API.Data.Context;

/// <summary>
/// Sessão de acesso ao banco de dados da aplicação.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, TimeProvider timeProvider) : DbContext(options)
{
    /// <summary>
    /// Conjunto de usuários.
    /// </summary>
    public DbSet<UserModel> Users => Set<UserModel>();

    /// <summary>
    /// Conjunto de questões.
    /// </summary>
    public DbSet<QuestionModel> Questions => Set<QuestionModel>();

    /// <summary>
    /// Conjunto de tarefas.
    /// </summary>
    public DbSet<TaskModel> Tasks => Set<TaskModel>();

    /// <summary>
    /// Persiste as alterações e atualiza os dados comuns das entidades.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação.</param>
    /// <returns>Quantidade de registros afetados.</returns>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetUpdatedAt();

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Aplica as configurações das entidades ao modelo do banco de dados.
    /// </summary>
    /// <param name="modelBuilder">Construtor utilizado para configurar o modelo.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>
    /// Define a data de atualização das entidades modificadas.
    /// </summary>
    private void SetUpdatedAt()
    {
        var now = timeProvider.GetUtcNow();
        var changedEntries = ChangeTracker
            .Entries<BaseModel>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in changedEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = entry.Entity.CreatedAt.ToUniversalTime();
            }
            else
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
